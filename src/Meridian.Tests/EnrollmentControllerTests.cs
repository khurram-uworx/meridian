using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Meridian.Controllers;
using Meridian.Models;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;

namespace Meridian.Tests;

[TestFixture]
public class EnrollmentControllerTests
{
    private Mock<IEnrollmentService> _enrollmentServiceMock = null!;
    private Mock<IOptions<JiraOptions>> _jiraOptionsMock = null!;
    private Mock<ILogger<EnrollmentController>> _loggerMock = null!;
    private EnrollmentController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _enrollmentServiceMock = new Mock<IEnrollmentService>();
        _jiraOptionsMock = new Mock<IOptions<JiraOptions>>();
        _loggerMock = new Mock<ILogger<EnrollmentController>>();

        _jiraOptionsMock.Setup(o => o.Value).Returns(new JiraOptions { BaseUrl = "https://jira.example.com" });

        _controller = new EnrollmentController(
            _enrollmentServiceMock.Object,
            _jiraOptionsMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public void Index_Get_ReturnsView()
    {
        // Act
        var result = _controller.Index();

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
            SourceType = CourseSourceType.GitRoot,
            SourceUri = "https://github.com/test/course"
        };

        var enrollment = new Enrollment { JiraEpicKey = "PROJ-1" };
        _enrollmentServiceMock.Setup(s => s.EnrollAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _controller.Index(model);

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
        _controller.ModelState.AddModelError("LearnerEmail", "Required");
        var model = new EnrollmentViewModel();

        // Act
        var result = await _controller.Index(model);

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
            SourceType = CourseSourceType.GitRoot,
            SourceUri = "https://github.com/test/course"
        };

        _enrollmentServiceMock.Setup(s => s.EnrollAsync(model.LearnerEmail, It.IsAny<CourseSourceLocator>()))
            .ThrowsAsync(new Exception("Something went wrong"));

        // Act
        var result = await _controller.Index(model);

        // Assert
        Assert.That(result, Is.InstanceOf<ViewResult>());
        var viewResult = (ViewResult)result;
        Assert.That(_controller.ModelState.IsValid, Is.False);
        Assert.That(_controller.ModelState[string.Empty]?.Errors[0].ErrorMessage, Does.Contain("Something went wrong"));
    }

    [Test]
    public void Confirm_ValidEpicKey_ReturnsViewWithData()
    {
        // Act
        var result = _controller.Confirm("PROJ-1");

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
        var result = _controller.Confirm(string.Empty);

        // Assert
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirectResult = (RedirectToActionResult)result;
        Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
    }
}
