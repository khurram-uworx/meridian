using Meridian.Controllers;
using Meridian.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class EnrollmentControllerTests
{
    Mock<IEnrollmentOperationService> enrollmentOperationServiceMock = null!;
    Mock<IEnrollmentQueue> enrollmentQueueMock = null!;
    Mock<IOptions<JiraOptions>> jiraOptionsMock = null!;
    Mock<ILogger<EnrollmentController>> loggerMock = null!;
    EnrollmentController controller = null!;

    [SetUp]
    public void SetUp()
    {
        enrollmentOperationServiceMock = new Mock<IEnrollmentOperationService>();
        enrollmentQueueMock = new Mock<IEnrollmentQueue>();
        jiraOptionsMock = new Mock<IOptions<JiraOptions>>();
        loggerMock = new Mock<ILogger<EnrollmentController>>();

        jiraOptionsMock.Setup(o => o.Value).Returns(new JiraOptions { BaseUrl = "https://jira.example.com" });

        controller = new EnrollmentController(
            enrollmentOperationServiceMock.Object,
            enrollmentQueueMock.Object,
            jiraOptionsMock.Object,
            loggerMock.Object);
    }

    [Test]
    public void Index_Get_ReturnsView()
    {
        // Act
        var result = controller.Index();

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_Post_ValidModel_QueuesAndRedirectsToProgress()
    {
        // Arrange
        var model = new EnrollmentViewModel
        {
            LearnerEmail = "test@example.com",
            SourceType = CourseSourceType.Git,
            SourceUri = "https://github.com/test/course"
        };

        var operationId = Guid.NewGuid();
        var operation = new EnrollmentOperationSnapshot(
            operationId,
            model.LearnerEmail,
            model.SourceType.ToString(),
            model.SourceUri,
            null,
            "queued",
            "queued",
            "Enrollment request accepted.",
            false,
            2,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            Array.Empty<EnrollmentOperationEventSnapshot>());

        enrollmentOperationServiceMock
            .Setup(s => s.CreateQueuedAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ReturnsAsync(operation);
        enrollmentQueueMock
            .Setup(q => q.EnqueueAsync(operationId, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await controller.Index(model, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirectResult = (RedirectToActionResult)result;
        Assert.That(redirectResult.ActionName, Is.EqualTo("Progress"));
        Assert.That(redirectResult.RouteValues?["operationId"], Is.EqualTo(operationId));
    }

    [Test]
    public async Task Index_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        controller.ModelState.AddModelError("LearnerEmail", "Required");
        var model = new EnrollmentViewModel();

        // Act
        var result = await controller.Index(model, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.Model, Is.EqualTo(model));
    }

    [Test]
    public async Task Index_Post_StartFails_ReturnsViewWithError()
    {
        // Arrange
        var model = new EnrollmentViewModel
        {
            LearnerEmail = "test@example.com",
            SourceType = CourseSourceType.Git,
            SourceUri = "https://github.com/test/course"
        };

        enrollmentOperationServiceMock.Setup(s => s.CreateQueuedAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ThrowsAsync(new Exception("Something went wrong"));

        // Act
        var result = await controller.Index(model, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(controller.ModelState.IsValid, Is.False);
        Assert.That(controller.ModelState[string.Empty]?.Errors[0].ErrorMessage, Does.Contain("Something went wrong"));
    }

    [Test]
    public void Confirm_ValidEpicKey_ReturnsViewWithData()
    {
        // Act
        var result = controller.Confirm("PROJ-1");

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.ViewData["EpicKey"], Is.EqualTo("PROJ-1"));
        Assert.That(viewResult.ViewData["JiraBaseUrl"], Is.EqualTo("https://jira.example.com"));
    }

    [Test]
    public void Confirm_EmptyEpicKey_RedirectsToIndex()
    {
        // Act
        var result = controller.Confirm(string.Empty);

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirectResult = (RedirectToActionResult)result;
        Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public async Task Progress_UnknownOperation_RedirectsToIndex()
    {
        var operationId = Guid.NewGuid();
        enrollmentOperationServiceMock
            .Setup(s => s.GetSnapshotAsync(operationId))
            .ReturnsAsync((EnrollmentOperationSnapshot?)null);

        var result = await controller.Progress(operationId);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = (RedirectToActionResult)result;
        Assert.That(redirect.ActionName, Is.EqualTo("Index"));
    }

    [Test]
    public async Task Start_ValidModel_ReturnsAcceptedWithOperationId()
    {
        var model = new EnrollmentViewModel
        {
            LearnerEmail = "test@example.com",
            SourceType = CourseSourceType.Git,
            SourceUri = "https://github.com/test/course"
        };

        var operation = new EnrollmentOperationSnapshot(
            Guid.NewGuid(),
            model.LearnerEmail,
            model.SourceType.ToString(),
            model.SourceUri,
            null,
            "queued",
            "queued",
            "Enrollment request accepted.",
            false,
            2,
            null,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            Array.Empty<EnrollmentOperationEventSnapshot>());

        enrollmentOperationServiceMock
            .Setup(s => s.CreateQueuedAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ReturnsAsync(operation);
        enrollmentQueueMock
            .Setup(q => q.EnqueueAsync(operation.Id, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var result = await controller.Start(model, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<AcceptedResult>());
        var accepted = (AcceptedResult)result;
        Assert.That(accepted.Value, Is.Not.Null);
    }

    [Test]
    public async Task Status_UnknownOperation_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        enrollmentOperationServiceMock
            .Setup(s => s.GetSnapshotAsync(id))
            .ReturnsAsync((EnrollmentOperationSnapshot?)null);

        var result = await controller.Status(id);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
