using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Uworx.Meridian;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;

namespace Meridian.Controllers;

public class QuizController : Controller
{
    private readonly MeridianDbContext _dbContext;
    private readonly ICourseParser _courseParser;
    private readonly IJiraService _jiraService;
    private readonly ILogger<QuizController> _logger;

    public QuizController(
        MeridianDbContext dbContext,
        ICourseParser courseParser,
        IJiraService jiraService,
        ILogger<QuizController> logger)
    {
        _dbContext = dbContext;
        _courseParser = courseParser;
        _jiraService = jiraService;
        _logger = logger;
    }

    [HttpGet("/quiz/{quizId}")]
    public async Task<IActionResult> Index(string quizId, [FromQuery] int enrollment)
    {
        var enrollmentRecord = await _dbContext.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollment);

        if (enrollmentRecord == null)
        {
            return NotFound("Enrollment not found.");
        }

        var source = new CourseSourceLocator(
            Enum.Parse<CourseSourceType>(enrollmentRecord.Course.SourceType),
            enrollmentRecord.Course.SourceLocator,
            enrollmentRecord.Course.CoursePath
        );

        var parsedCourse = await _courseParser.ParseAsync(source);
        var section = parsedCourse.Sections.FirstOrDefault(s => s.QuizId == quizId && s.Type == "quiz");

        if (section == null)
        {
            return NotFound("Quiz not found in this course.");
        }

        ViewBag.EnrollmentId = enrollment;
        ViewBag.QuizId = quizId;
        ViewBag.CourseTitle = parsedCourse.Config.Title;
        ViewBag.QuizTitle = section.Title;

        return View(section.QuizQuestions);
    }

    [HttpPost("/quiz/{quizId}")]
    public async Task<IActionResult> Submit(string quizId, [FromQuery] int enrollment, IFormCollection form)
    {
        var enrollmentRecord = await _dbContext.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Learner)
            .FirstOrDefaultAsync(e => e.Id == enrollment);

        if (enrollmentRecord == null)
        {
            return NotFound("Enrollment not found.");
        }

        var source = new CourseSourceLocator(
            Enum.Parse<CourseSourceType>(enrollmentRecord.Course.SourceType),
            enrollmentRecord.Course.SourceLocator,
            enrollmentRecord.Course.CoursePath
        );

        var parsedCourse = await _courseParser.ParseAsync(source);
        var section = parsedCourse.Sections.FirstOrDefault(s => s.QuizId == quizId && s.Type == "quiz");

        if (section == null)
        {
            return NotFound("Quiz not found.");
        }

        int score = 0;
        var questions = section.QuizQuestions.ToList();
        for (int i = 0; i < questions.Count; i++)
        {
            if (int.TryParse(form[$"question_{i}"], out int selectedIndex) && selectedIndex == questions[i].CorrectIndex)
            {
                score++;
            }
        }

        // 1. Persist QuizAttempt
        var attempt = new QuizAttempt
        {
            EnrollmentId = enrollment,
            QuizId = quizId,
            Score = score,
            MaxScore = questions.Count,
            AttemptedAt = DateTime.UtcNow
        };

        // 2. Jira Integration
        try
        {
            var storyKey = await _jiraService.FindStoryKeyByLabelAsync(enrollmentRecord.JiraEpicKey, quizId);
            if (!string.IsNullOrEmpty(storyKey))
            {
                var comment = $"Quiz Attempt Details:\n" +
                              $"Learner: {enrollmentRecord.Learner.Email}\n" +
                              $"Score: {score} / {questions.Count}\n" +
                              $"Attempted At: {attempt.AttemptedAt:yyyy-MM-dd HH:mm:ss}";

                attempt.JiraCommentId = await _jiraService.PostCommentAsync(storyKey, comment);
                await _jiraService.TransitionToAsync(storyKey, "In Review");
            }
            else
            {
                _logger.LogWarning("Could not find Jira Story for quiz {QuizId} in epic {EpicKey}", quizId, enrollmentRecord.JiraEpicKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Jira for quiz submission.");
        }

        _dbContext.QuizAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync();

        return View("Finished", attempt);
    }
}
