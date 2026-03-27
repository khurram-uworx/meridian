using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Meridian.Models;
using Uworx.Meridian;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Uworx.Meridian.CourseSource;

namespace Meridian.Controllers;

public class LearnerController : Controller
{
    private readonly MeridianDbContext _dbContext;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IJiraService _jiraService;
    private readonly JiraOptions _jiraOptions;
    private readonly ILogger<LearnerController> _logger;

    public LearnerController(
        MeridianDbContext dbContext,
        IEnrollmentService enrollmentService,
        IJiraService jiraService,
        IOptions<JiraOptions> jiraOptions,
        ILogger<LearnerController> logger)
    {
        _dbContext = dbContext;
        _enrollmentService = enrollmentService;
        _jiraService = jiraService;
        _jiraOptions = jiraOptions.Value;
        _logger = logger;
    }

    [HttpGet("/learner/{learnerId}/progress")]
    public async Task<IActionResult> Progress(int learnerId)
    {
        var learner = await _dbContext.Learners.FindAsync(learnerId);
        if (learner == null)
        {
            return NotFound();
        }

        var enrollments = await _enrollmentService.GetEnrollmentsByLearnerIdAsync(learnerId);

        var viewModel = new LearnerProgressViewModel
        {
            LearnerId = learner.Id,
            LearnerName = learner.Name,
            Courses = new List<CourseProgressViewModel>()
        };

        foreach (var enrollment in enrollments)
        {
            var courseConfig = JsonSerializer.Deserialize<CourseConfig>(enrollment.Course.CourseYamlSnapshot);
            var courseProgress = new CourseProgressViewModel
            {
                CourseTitle = courseConfig?.Title ?? "Unknown Course",
                EpicKey = enrollment.JiraEpicKey
            };

            try
            {
                var stories = (await _jiraService.GetStoriesForEpicAsync(enrollment.JiraEpicKey)).ToList();
                courseProgress.Stories = stories;

                if (stories.Any())
                {
                    var doneCount = stories.Count(s => s.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));
                    courseProgress.CompletionPct = Math.Round((double)doneCount / stories.Count * 100, 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Jira stories for Epic {EpicKey}", enrollment.JiraEpicKey);
                courseProgress.ErrorMessage = "Could not retrieve progress from Jira.";
            }

            viewModel.Courses.Add(courseProgress);
        }

        ViewData["JiraBaseUrl"] = _jiraOptions.BaseUrl;
        return View(viewModel);
    }

    [HttpGet("/learner/{learnerId}/history")]
    public async Task<IActionResult> History(int learnerId)
    {
        var learner = await _dbContext.Learners.FindAsync(learnerId);
        if (learner == null)
        {
            return NotFound();
        }

        var enrollments = (await _enrollmentService.GetEnrollmentsByLearnerIdAsync(learnerId))
            .OrderByDescending(e => e.EnrolledAt)
            .ToList();

        var viewModel = new EnrollmentHistoryViewModel
        {
            LearnerId = learner.Id,
            LearnerName = learner.Name,
            Enrollments = new List<EnrollmentHistoryItemViewModel>()
        };

        foreach (var enrollment in enrollments)
        {
            var courseConfig = JsonSerializer.Deserialize<CourseConfig>(enrollment.Course.CourseYamlSnapshot);
            var historyItem = new EnrollmentHistoryItemViewModel
            {
                CourseTitle = courseConfig?.Title ?? "Unknown Course",
                EnrolledAt = enrollment.EnrolledAt,
                JiraEpicKey = enrollment.JiraEpicKey
            };

            try
            {
                var stories = (await _jiraService.GetStoriesForEpicAsync(enrollment.JiraEpicKey)).ToList();
                if (stories.Any())
                {
                    var doneCount = stories.Count(s => s.Status.Equals("Done", StringComparison.OrdinalIgnoreCase));
                    historyItem.CompletionPct = Math.Round((double)doneCount / stories.Count * 100, 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Jira stories for Epic {EpicKey}", enrollment.JiraEpicKey);
                historyItem.ErrorMessage = "Could not retrieve progress from Jira.";
            }

            viewModel.Enrollments.Add(historyItem);
        }

        ViewData["JiraBaseUrl"] = _jiraOptions.BaseUrl;
        return View(viewModel);
    }
}
