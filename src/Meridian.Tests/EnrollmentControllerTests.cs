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
using Uworx.Meridian.Entities;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class EnrollmentControllerTests
{
    Mock<IEnrollmentService> enrollmentServiceMock = null!;
    Mock<IOptions<JiraOptions>> jiraOptionsMock = null!;
    Mock<ILogger<EnrollmentController>> loggerMock = null!;
    EnrollmentController controller = null!;

    [SetUp]
    public void SetUp()
    {
        enrollmentServiceMock = new Mock<IEnrollmentService>();
        jiraOptionsMock = new Mock<IOptions<JiraOptions>>();
        loggerMock = new Mock<ILogger<EnrollmentController>>();

        jiraOptionsMock.Setup(o => o.Value).Returns(new JiraOptions { BaseUrl = "https://jira.example.com" });

        controller = new EnrollmentController(
            enrollmentServiceMock.Object,
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
    public async Task Index_Post_ValidModel_RedirectsToConfirm()
    {
        // Arrange
        var model = new EnrollmentViewModel
        {
            LearnerEmail = "test@example.com",
            SourceType = CourseSourceType.Git,
            SourceUri = "https://github.com/test/course"
        };

        var enrollment = new Enrollment { JiraEpicKey = "PROJ-1" };
        enrollmentServiceMock.Setup(s => s.EnrollAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ReturnsAsync(enrollment);

        // Act
        var result = await controller.Index(model);

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirectResult = (RedirectToActionResult)result;
        Assert.That(redirectResult.ActionName, Is.EqualTo("Confirm"));
        Assert.That(redirectResult.RouteValues?["epicKey"], Is.EqualTo("PROJ-1"));
    }

    [Test]
    public async Task Index_Post_InvalidModel_ReturnsViewWithModel()
    {
        // Arrange
        controller.ModelState.AddModelError("LearnerEmail", "Required");
        var model = new EnrollmentViewModel();

        // Act
        var result = await controller.Index(model);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(viewResult.Model, Is.EqualTo(model));
    }

    [Test]
    public async Task Index_Post_ServiceThrows_ReturnsViewWithError()
    {
        // Arrange
        var model = new EnrollmentViewModel
        {
            LearnerEmail = "test@example.com",
            SourceType = CourseSourceType.Git,
            SourceUri = "https://github.com/test/course"
        };

        enrollmentServiceMock.Setup(s => s.EnrollAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ThrowsAsync(new Exception("Something went wrong"));

        // Act
        var result = await controller.Index(model);

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
}
