using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Uworx.Meridian;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure;
using Uworx.Meridian.Infrastructure.Data;

namespace Meridian.Tests;

[TestFixture]
public class EnrollmentServiceTests
{
    private MeridianDbContext _dbContext = null!;
    private Mock<ICourseParser> _courseParserMock = null!;
    private Mock<IJiraService> _jiraServiceMock = null!;
    private Mock<ILogger<EnrollmentService>> _loggerMock = null!;
    private EnrollmentService _enrollmentService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MeridianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new MeridianDbContext(options);

        _courseParserMock = new Mock<ICourseParser>();
        _jiraServiceMock = new Mock<IJiraService>();
        _loggerMock = new Mock<ILogger<EnrollmentService>>();

        _enrollmentService = new EnrollmentService(
            _dbContext,
            _courseParserMock.Object,
            _jiraServiceMock.Object,
            _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task EnrollAsync_HappyPath_CreatesEverything()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian");
        var sections = new List<SectionDefinition>
        {
            new SectionDefinition("Intro", 1, "lesson", 3, null, null, "Intro Body"),
            new SectionDefinition("Setup", 2, "lesson", 5, null, null, "Setup Body")
        };
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        _courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        _jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-1");

        _jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-2");

        // Act
        var result = await _enrollmentService.EnrollAsync(learnerEmail, source);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JiraEpicKey, Is.EqualTo("LEARN-1"));
            Assert.That(result.SourceRevision, Is.EqualTo("rev123"));

            var learner = _dbContext.Learners.Single();
            Assert.That(learner.Email, Is.EqualTo(learnerEmail));

            var course = _dbContext.Courses.Single();
            Assert.That(course.SourceLocator, Is.EqualTo(source.Uri));

            var enrollment = _dbContext.Enrollments.Single();
            Assert.That(enrollment.Id, Is.EqualTo(result.Id));
        });

        _jiraServiceMock.Verify(j => j.CreateEpicAsync("LEARN", "Test Course", "meridian"), Times.Once);
        _jiraServiceMock.Verify(j => j.CreateStoryAsync("LEARN-1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public async Task EnrollAsync_JiraStoryFails_DoesNotPersistEnrollment()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian");
        var sections = new List<SectionDefinition>
        {
            new SectionDefinition("Intro", 1, "lesson", 3, null, null, "Intro Body"),
            new SectionDefinition("Fail", 2, "lesson", 5, null, null, "Fail Body")
        };
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        _courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        _jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-1");

        _jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), "Intro", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-2");

        _jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), "Fail", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Jira Error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await _enrollmentService.EnrollAsync(learnerEmail, source));

        Assert.That(_dbContext.Enrollments.Count(), Is.Zero);
        // Learner and Course might be persisted because SaveChangesAsync is called before Jira operations
        // and they are not part of the same transaction (In-Memory provider doesn't support transactions in the same way).
        // However, the Enrollment record MUST NOT be there.
    }

    [Test]
    public async Task EnrollAsync_LearnerUpsert_IsIdempotent()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        _dbContext.Learners.Add(new Learner { Email = learnerEmail, Name = "Existing", JiraAccountId = "existing" });
        await _dbContext.SaveChangesAsync();

        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian");
        var sections = new List<SectionDefinition>();
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        _courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        _jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-1");

        // Act
        await _enrollmentService.EnrollAsync(learnerEmail, source);

        // Assert
        Assert.That(_dbContext.Learners.Count(), Is.EqualTo(1));
        Assert.That(_dbContext.Learners.Single().Email, Is.EqualTo(learnerEmail));
    }
}
