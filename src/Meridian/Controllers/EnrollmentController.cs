using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Meridian.Models;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;

namespace Meridian.Controllers;

public class EnrollmentController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly JiraOptions _jiraOptions;
    private readonly ILogger<EnrollmentController> _logger;

    public EnrollmentController(
        IEnrollmentService enrollmentService,
        IOptions<JiraOptions> jiraOptions,
        ILogger<EnrollmentController> logger)
    {
        _enrollmentService = enrollmentService;
        _jiraOptions = jiraOptions.Value;
        _logger = logger;
    }

    [HttpGet("/enroll")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("/enroll")]
    public async Task<IActionResult> Index(EnrollmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var source = new CourseSourceLocator(model.SourceType, model.SourceUri, model.SubPath);
            var enrollment = await _enrollmentService.EnrollAsync(model.LearnerEmail, source);

            return RedirectToAction(nameof(Confirm), new { epicKey = enrollment.JiraEpicKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during enrollment for {Email}", model.LearnerEmail);
            ModelState.AddModelError(string.Empty, $"An error occurred during enrollment: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet("/enroll/confirm")]
    public IActionResult Confirm(string epicKey)
    {
        if (string.IsNullOrEmpty(epicKey))
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["EpicKey"] = epicKey;
        ViewData["JiraBaseUrl"] = _jiraOptions.BaseUrl;

        return View();
    }
}
