using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Meridian.Controllers;
using Meridian.Models;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Uworx.Meridian.CourseSource;

namespace Meridian.Tests;

[TestFixture]
public class LearnerControllerTests
{
    private MeridianDbContext _dbContext = null!;
    private Mock<IEnrollmentService> _enrollmentServiceMock = null!;
    private Mock<IJiraService> _jiraServiceMock = null!;
    private Mock<IOptions<JiraOptions>> _jiraOptionsMock = null!;
    private Mock<ILogger<LearnerController>> _loggerMock = null!;
    private LearnerController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MeridianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new MeridianDbContext(options);

        _enrollmentServiceMock = new Mock<IEnrollmentService>();
        _jiraServiceMock = new Mock<IJiraService>();
        _jiraOptionsMock = new Mock<IOptions<JiraOptions>>();
        _loggerMock = new Mock<ILogger<LearnerController>>();

        _jiraOptionsMock.Setup(o => o.Value).Returns(new JiraOptions { BaseUrl = "https://jira.example.com" });

        _controller = new LearnerController(
            _dbContext,
            _enrollmentServiceMock.Object,
            _jiraServiceMock.Object,
            _jiraOptionsMock.Object,
            _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task History_LearnerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _controller.History(999);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task History_ValidLearner_ReturnsViewWithEnrollments()
    {
        // Arrange
        var learner = new Learner { Id = 1, Name = "Test Learner", Email = "test@example.com" };
        _dbContext.Learners.Add(learner);

        var courseConfig = new CourseConfig("C1", "Course 1", "1.0", "Author", "PROJ", "label");
        var course = new Course
        {
            Id = 1,
            SourceType = "GitRoot",
            SourceLocator = "http://repo",
            CourseYamlSnapshot = JsonSerializer.Serialize(courseConfig)
        };
        _dbContext.Courses.Add(course);

        var enrollment1 = new Enrollment
        {
            Id = 1,
            LearnerId = 1,
            CourseId = 1,
            JiraEpicKey = "EPIC-1",
            EnrolledAt = DateTime.UtcNow.AddDays(-1),
            Course = course
        };
        var enrollment2 = new Enrollment
        {
            Id = 2,
            LearnerId = 1,
            CourseId = 1,
            JiraEpicKey = "EPIC-2",
            EnrolledAt = DateTime.UtcNow,
            Course = course
        };

        _dbContext.Enrollments.AddRange(enrollment1, enrollment2);
        await _dbContext.SaveChangesAsync();

        _enrollmentServiceMock.Setup(s => s.GetEnrollmentsByLearnerIdAsync(1))
            .ReturnsAsync(new List<Enrollment> { enrollment1, enrollment2 });

        _jiraServiceMock.Setup(s => s.GetStoriesForEpicAsync("EPIC-1"))
            .ReturnsAsync(new List<JiraStoryStatus>
            {
                new JiraStoryStatus("STORY-1", "Story 1", "Done", 3)
            });

        _jiraServiceMock.Setup(s => s.GetStoriesForEpicAsync("EPIC-2"))
            .ReturnsAsync(new List<JiraStoryStatus>
            {
                new JiraStoryStatus("STORY-2", "Story 2", "To Do", 5)
            });

        // Act
        var result = await _controller.History(1);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        var model = (EnrollmentHistoryViewModel)viewResult.Model!;

        Assert.That(model.LearnerId, Is.EqualTo(1));
        Assert.That(model.Enrollments.Count, Is.EqualTo(2));

        // Ordered by descending EnrolledAt
        Assert.That(model.Enrollments[0].JiraEpicKey, Is.EqualTo("EPIC-2"));
        Assert.That(model.Enrollments[0].CompletionPct, Is.EqualTo(0));

        Assert.That(model.Enrollments[1].JiraEpicKey, Is.EqualTo("EPIC-1"));
        Assert.That(model.Enrollments[1].CompletionPct, Is.EqualTo(100));
    }

    [Test]
    public async Task History_JiraError_SetsErrorMessage()
    {
        // Arrange
        var learner = new Learner { Id = 1, Name = "Test Learner", Email = "test@example.com" };
        _dbContext.Learners.Add(learner);

        var courseConfig = new CourseConfig("C1", "Course 1", "1.0", "Author", "PROJ", "label");
        var course = new Course
        {
            Id = 1,
            CourseYamlSnapshot = JsonSerializer.Serialize(courseConfig)
        };
        _dbContext.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = 1,
            LearnerId = 1,
            CourseId = 1,
            JiraEpicKey = "EPIC-1",
            EnrolledAt = DateTime.UtcNow,
            Course = course
        };

        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        _enrollmentServiceMock.Setup(s => s.GetEnrollmentsByLearnerIdAsync(1))
            .ReturnsAsync(new List<Enrollment> { enrollment });

        _jiraServiceMock.Setup(s => s.GetStoriesForEpicAsync("EPIC-1"))
            .ThrowsAsync(new Exception("Jira is down"));

        // Act
        var result = await _controller.History(1);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        var model = (EnrollmentHistoryViewModel)viewResult.Model!;

        Assert.That(model.Enrollments[0].ErrorMessage, Is.EqualTo("Could not retrieve progress from Jira."));
    }
}
