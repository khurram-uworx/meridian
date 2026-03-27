using Meridian.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;

namespace Meridian.Controllers;

public class EnrollmentController : Controller
{
    readonly IEnrollmentService enrollmentService;
    readonly JiraOptions jiraOptions;
    readonly ILogger<EnrollmentController> logger;

    public EnrollmentController(
        IEnrollmentService enrollmentService,
        IOptions<JiraOptions> jiraOptions,
        ILogger<EnrollmentController> logger)
    {
        this.enrollmentService = enrollmentService;
        this.jiraOptions = jiraOptions.Value;
        this.logger = logger;
    }

    [HttpGet("/enroll")]
    public IActionResult Index() => View();

    [HttpPost("/enroll")]
    public async Task<IActionResult> Index(EnrollmentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var source = new CourseSourceLocator(model.SourceType, model.SourceUri, model.SubPath);
            var enrollment = await enrollmentService.EnrollAsync(model.LearnerEmail, source);

            return RedirectToAction(nameof(Confirm), new { epicKey = enrollment.JiraEpicKey });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during enrollment for {Email}", model.LearnerEmail);
            ModelState.AddModelError(string.Empty, $"An error occurred during enrollment: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet("/enroll/confirm")]
    public IActionResult Confirm(string epicKey)
    {
        if (string.IsNullOrEmpty(epicKey))
            return RedirectToAction(nameof(Index));

        ViewData["EpicKey"] = epicKey;
        ViewData["JiraBaseUrl"] = jiraOptions.BaseUrl;

        return View();
    }
}
