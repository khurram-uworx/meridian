using Meridian.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;

namespace Meridian.Controllers;

public class EnrollmentController : Controller
{
    readonly IEnrollmentOperationService enrollmentOperationService;
    readonly IEnrollmentQueue enrollmentQueue;
    readonly JiraOptions jiraOptions;
    readonly ILogger<EnrollmentController> logger;

    public EnrollmentController(
        IEnrollmentOperationService enrollmentOperationService,
        IEnrollmentQueue enrollmentQueue,
        IOptions<JiraOptions> jiraOptions,
        ILogger<EnrollmentController> logger)
    {
        this.enrollmentOperationService = enrollmentOperationService;
        this.enrollmentQueue = enrollmentQueue;
        this.jiraOptions = jiraOptions.Value;
        this.logger = logger;
    }

    [HttpGet("/enroll")]
    public IActionResult Index() => View();

    [HttpPost("/enroll")]
    public async Task<IActionResult> Index(EnrollmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var source = new CourseSourceLocator(model.SourceType, model.SourceUri, model.SubPath);
            var operation = await enrollmentOperationService.CreateQueuedAsync(model.LearnerEmail, source);
            await enrollmentQueue.EnqueueAsync(operation.Id, cancellationToken);
            return RedirectToAction(nameof(Progress), new { operationId = operation.Id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scheduling enrollment for {Email}", model.LearnerEmail);
            ModelState.AddModelError(string.Empty, $"Failed to start enrollment: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet("/enroll/progress/{operationId:guid}")]
    public async Task<IActionResult> Progress(Guid operationId)
    {
        var snapshot = await enrollmentOperationService.GetSnapshotAsync(operationId);
        if (snapshot == null)
            return RedirectToAction(nameof(Index));

        ViewData["OperationId"] = operationId;
        return View();
    }

    [HttpPost("/enroll/start")]
    public async Task<IActionResult> Start(EnrollmentViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var source = new CourseSourceLocator(model.SourceType, model.SourceUri, model.SubPath);
            var operation = await enrollmentOperationService.CreateQueuedAsync(model.LearnerEmail, source);
            await enrollmentQueue.EnqueueAsync(operation.Id, cancellationToken);

            return Accepted(new
            {
                operationId = operation.Id,
                statusUrl = $"/enroll/status/{operation.Id}",
                confirmUrlTemplate = "/enroll/confirm?epicKey=__EPIC_KEY__"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error scheduling enrollment for {Email}", model.LearnerEmail);
            return Problem($"Failed to start enrollment: {ex.Message}");
        }
    }

    [HttpGet("/enroll/status/{operationId:guid}")]
    public async Task<IActionResult> Status(Guid operationId)
    {
        var snapshot = await enrollmentOperationService.GetSnapshotAsync(operationId);
        if (snapshot == null)
            return NotFound();

        return Ok(snapshot);
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
