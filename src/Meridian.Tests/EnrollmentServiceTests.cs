using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure;
using Uworx.Meridian.Infrastructure.Data;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class EnrollmentServiceTests
{
    MeridianDbContext dbContext = null!;
    Mock<ICourseParser> courseParserMock = null!;
    Mock<IJiraService> jiraServiceMock = null!;
    Mock<ILogger<EnrollmentService>> loggerMock = null!;
    EnrollmentService enrollmentService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MeridianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new MeridianDbContext(options);

        var jiraOptionsMock = new Mock<IOptions<JiraOptions>>();
        jiraOptionsMock.Setup(o => o.Value).Returns(new JiraOptions { ProjectKey = "LEARN" });

        courseParserMock = new Mock<ICourseParser>();
        jiraServiceMock = new Mock<IJiraService>();
        loggerMock = new Mock<ILogger<EnrollmentService>>();

        enrollmentService = new EnrollmentService(
            jiraOptionsMock.Object,
            dbContext,
            courseParserMock.Object,
            jiraServiceMock.Object,
            loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        dbContext.Dispose();
    }

    [Test]
    public async Task EnrollAsync_HappyPath_CreatesEverything()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian", "Course intro");
        var sections = new List<SectionDefinition>
        {
            new SectionDefinition("Intro", 1, 3, null, null, "Intro Body"),
            new SectionDefinition("Setup", 2, 5, null, null, "Setup Body")
        };
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("LEARN-1");

        jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-2");

        // Act
        var result = await enrollmentService.EnrollAsync(learnerEmail, source);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.JiraEpicKey, Is.EqualTo("LEARN-1"));
            Assert.That(result.SourceRevision, Is.EqualTo("rev123"));

            var learner = dbContext.Learners.Single();
            Assert.That(learner.Email, Is.EqualTo(learnerEmail));

            var course = dbContext.Courses.Single();
            Assert.That(course.SourceLocator, Is.EqualTo(source.Uri));

            var enrollment = dbContext.Enrollments.Single();
            Assert.That(enrollment.Id, Is.EqualTo(result.Id));
        });

        jiraServiceMock.Verify(j => j.CreateEpicAsync("LEARN", "Test Course", "meridian", "Course intro"), Times.Once);
        jiraServiceMock.Verify(j => j.CreateStoryAsync("LEARN-1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), "section"), Times.Exactly(2));
    }

    [Test]
    public async Task EnrollAsync_JiraStoryFails_DoesNotPersistEnrollment()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian", null);
        var sections = new List<SectionDefinition>
        {
            new SectionDefinition("Intro", 1, 3, null, null, "Intro Body"),
            new SectionDefinition("Fail", 2, 5, null, null, "Fail Body")
        };
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("LEARN-1");

        jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), "Intro", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-2");

        jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), "Fail", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Jira Error"));

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => await enrollmentService.EnrollAsync(learnerEmail, source));

        Assert.That(dbContext.Enrollments.Count(), Is.Zero);
        // Learner and Course might be persisted because SaveChangesAsync is called before Jira operations
        // and they are not part of the same transaction (In-Memory provider doesn't support transactions in the same way).
        // However, the Enrollment record MUST NOT be there.
    }

    [Test]
    public async Task EnrollAsync_LearnerUpsert_IsIdempotent()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        dbContext.Learners.Add(new Learner { Email = learnerEmail, Name = "Existing", JiraAccountId = "existing" });
        await dbContext.SaveChangesAsync();

        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian", null);
        var sections = new List<SectionDefinition>();
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("LEARN-1");

        // Act
        await enrollmentService.EnrollAsync(learnerEmail, source);

        // Assert
        Assert.That(dbContext.Learners.Count(), Is.EqualTo(1));
        Assert.That(dbContext.Learners.Single().Email, Is.EqualTo(learnerEmail));
    }

    [Test]
    public async Task GetEnrollmentsByLearnerIdAsync_ReturnsEnrollmentsWithCourse()
    {
        // Arrange
        var learner = new Learner { Email = "test@example.com", Name = "Test", JiraAccountId = "test" };
        dbContext.Learners.Add(learner);
        var course = new Course { SourceType = "Local", SourceLocator = "loc", CoursePath = "path", CourseYamlSnapshot = "{}" };
        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            LearnerId = learner.Id,
            CourseId = course.Id,
            JiraEpicKey = "LEARN-1",
            EnrolledAt = DateTime.UtcNow
        };
        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await enrollmentService.GetEnrollmentsByLearnerIdAsync(learner.Id);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().JiraEpicKey, Is.EqualTo("LEARN-1"));
        Assert.That(result.First().Course, Is.Not.Null);
    }

    [Test]
    public async Task EnrollAsync_SectionWithOptionalQuiz_PostsQuizLinkAsComment()
    {
        // Arrange
        var learnerEmail = "test@example.com";
        var source = new CourseSourceLocator(CourseSourceType.LocalFolder, "/path/to/course");
        var config = new CourseConfig("X102", "Test Course", "1.0", "Author", "LEARN", "meridian", null);
        var sections = new List<SectionDefinition>
        {
            new SectionDefinition(
                "Module 1",
                1,
                3,
                "module-1-quiz",
                null,
                "Lesson body",
                new[]
                {
                    new QuizQuestion("What is CI?", new List<string> { "A", "B" }, 0)
                })
        };
        var parsedCourse = new ParsedCourse(config, sections, "rev123");

        courseParserMock.Setup(p => p.ParseAsync(source))
            .ReturnsAsync(parsedCourse);

        jiraServiceMock.Setup(j => j.CreateEpicAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("LEARN-1");

        jiraServiceMock.Setup(j => j.CreateStoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync("LEARN-2");

        jiraServiceMock.Setup(j => j.PostCommentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("10001");

        // Act
        await enrollmentService.EnrollAsync(learnerEmail, source);

        // Assert
        jiraServiceMock.Verify(j => j.CreateStoryAsync(
                "LEARN-1",
                "Module 1",
                "Lesson body",
                3,
                "module-1-quiz"),
            Times.Once);

        jiraServiceMock.Verify(j => j.PostCommentAsync(
                "LEARN-2",
                It.Is<string>(comment => comment.Contains("/quiz/module-1-quiz?enrollment=", StringComparison.Ordinal))),
            Times.Once);
    }
}
