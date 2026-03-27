using Meridian.Controllers;
using Meridian.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Text.Json;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class LearnerControllerTests
{
    MeridianDbContext dbContext = null!;
    Mock<IEnrollmentService> enrollmentServiceMock = null!;
    Mock<IJiraService> jiraServiceMock = null!;
    Mock<IOptions<JiraOptions>> jiraOptionsMock = null!;
    Mock<ILogger<LearnerController>> loggerMock = null!;
    LearnerController controller = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<MeridianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        dbContext = new MeridianDbContext(options);

        enrollmentServiceMock = new Mock<IEnrollmentService>();
        jiraServiceMock = new Mock<IJiraService>();
        jiraOptionsMock = new Mock<IOptions<JiraOptions>>();
        loggerMock = new Mock<ILogger<LearnerController>>();

        jiraOptionsMock.Setup(o => o.Value).Returns(new JiraOptions { BaseUrl = "https://jira.example.com" });

        controller = new LearnerController(
            dbContext,
            enrollmentServiceMock.Object,
            jiraServiceMock.Object,
            jiraOptionsMock.Object,
            loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        dbContext.Dispose();
    }

    [Test]
    public async Task History_LearnerNotFound_ReturnsNotFound()
    {
        // Act
        var result = await controller.History(999);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task History_ValidLearner_ReturnsViewWithEnrollments()
    {
        // Arrange
        var learner = new Learner { Id = 1, Name = "Test Learner", Email = "test@example.com" };
        dbContext.Learners.Add(learner);

        var courseConfig = new CourseConfig("C1", "Course 1", "1.0", "Author", "PROJ", "label", null);
        var course = new Course
        {
            Id = 1,
            SourceType = "GitRoot",
            SourceLocator = "http://repo",
            CourseYamlSnapshot = JsonSerializer.Serialize(courseConfig)
        };
        dbContext.Courses.Add(course);

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

        dbContext.Enrollments.AddRange(enrollment1, enrollment2);
        await dbContext.SaveChangesAsync();

        enrollmentServiceMock.Setup(s => s.GetEnrollmentsByLearnerIdAsync(1))
            .ReturnsAsync(new List<Enrollment> { enrollment1, enrollment2 });

        jiraServiceMock.Setup(s => s.GetStoriesForEpicAsync("EPIC-1"))
            .ReturnsAsync(new List<JiraStoryStatus>
            {
                new JiraStoryStatus("STORY-1", "Story 1", "Done", 3)
            });

        jiraServiceMock.Setup(s => s.GetStoriesForEpicAsync("EPIC-2"))
            .ReturnsAsync(new List<JiraStoryStatus>
            {
                new JiraStoryStatus("STORY-2", "Story 2", "To Do", 5)
            });

        // Act
        var result = await controller.History(1);

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
        dbContext.Learners.Add(learner);

        var courseConfig = new CourseConfig("C1", "Course 1", "1.0", "Author", "PROJ", "label", null);
        var course = new Course
        {
            Id = 1,
            CourseYamlSnapshot = JsonSerializer.Serialize(courseConfig)
        };
        dbContext.Courses.Add(course);

        var enrollment = new Enrollment
        {
            Id = 1,
            LearnerId = 1,
            CourseId = 1,
            JiraEpicKey = "EPIC-1",
            EnrolledAt = DateTime.UtcNow,
            Course = course
        };

        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync();

        enrollmentServiceMock.Setup(s => s.GetEnrollmentsByLearnerIdAsync(1))
            .ReturnsAsync(new List<Enrollment> { enrollment });

        jiraServiceMock.Setup(s => s.GetStoriesForEpicAsync("EPIC-1"))
            .ThrowsAsync(new Exception("Jira is down"));

        // Act
        var result = await controller.History(1);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        var model = (EnrollmentHistoryViewModel)viewResult.Model!;

        Assert.That(model.Enrollments[0].ErrorMessage, Is.EqualTo("Could not retrieve progress from Jira."));
    }
}
