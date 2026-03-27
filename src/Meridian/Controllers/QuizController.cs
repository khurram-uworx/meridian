using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Uworx.Meridian;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;

namespace Meridian.Controllers;

public class QuizController : Controller
{
    readonly MeridianDbContext dbContext;
    readonly ICourseParser courseParser;
    readonly IJiraService jiraService;
    readonly ILogger<QuizController> logger;

    public QuizController(
        MeridianDbContext dbContext,
        ICourseParser courseParser,
        IJiraService jiraService,
        ILogger<QuizController> logger)
    {
        this.dbContext = dbContext;
        this.courseParser = courseParser;
        this.jiraService = jiraService;
        this.logger = logger;
    }

    [HttpGet("/quiz/{quizId}")]
    public async Task<IActionResult> Index(string quizId, [FromQuery] int enrollment)
    {
        var enrollmentRecord = await dbContext.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrollment);

        if (enrollmentRecord == null)
            return NotFound("Enrollment not found.");

        var source = new CourseSourceLocator(
            Enum.Parse<CourseSourceType>(enrollmentRecord.Course.SourceType),
            enrollmentRecord.Course.SourceLocator,
            enrollmentRecord.Course.CoursePath
        );

        var parsedCourse = await courseParser.ParseAsync(source);
        var section = parsedCourse.Sections.FirstOrDefault(s =>
            string.Equals(s.QuizId, quizId, StringComparison.OrdinalIgnoreCase) && s.QuizQuestions.Any());

        if (section == null)
            return NotFound("Quiz not found in this course.");

        ViewBag.EnrollmentId = enrollment;
        ViewBag.QuizId = quizId;
        ViewBag.CourseTitle = parsedCourse.Config.Title;
        ViewBag.QuizTitle = section.Title;

        return View(section.QuizQuestions);
    }

    [HttpPost("/quiz/{quizId}")]
    public async Task<IActionResult> Submit(string quizId, [FromQuery] int enrollment, IFormCollection form)
    {
        var enrollmentRecord = await dbContext.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Learner)
            .FirstOrDefaultAsync(e => e.Id == enrollment);

        if (enrollmentRecord == null)
            return NotFound("Enrollment not found.");

        var source = new CourseSourceLocator(
            Enum.Parse<CourseSourceType>(enrollmentRecord.Course.SourceType),
            enrollmentRecord.Course.SourceLocator,
            enrollmentRecord.Course.CoursePath
        );

        var parsedCourse = await courseParser.ParseAsync(source);
        var section = parsedCourse.Sections.FirstOrDefault(s =>
            string.Equals(s.QuizId, quizId, StringComparison.OrdinalIgnoreCase) && s.QuizQuestions.Any());

        if (section == null)
            return NotFound("Quiz not found.");

        int score = 0;
        var questions = section.QuizQuestions.ToList();
        for (int i = 0; i < questions.Count; i++)
            if (int.TryParse(form[$"question_{i}"], out int selectedIndex) && selectedIndex == questions[i].CorrectIndex)
                score++;

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
            var storyKey = await jiraService.FindStoryKeyByLabelAsync(enrollmentRecord.JiraEpicKey, quizId);
            if (!string.IsNullOrEmpty(storyKey))
            {
                var commentBuilder = new StringBuilder();
                commentBuilder.AppendLine("Quiz Attempt Details:");
                commentBuilder.AppendLine($"Section: {section.Title}");
                commentBuilder.AppendLine($"Quiz Id: {quizId}");
                commentBuilder.AppendLine($"Learner: {enrollmentRecord.Learner.Email}");
                commentBuilder.AppendLine($"Score: {score} / {questions.Count}");
                commentBuilder.AppendLine($"Attempted At: {attempt.AttemptedAt:yyyy-MM-dd HH:mm:ss} UTC");
                commentBuilder.AppendLine();
                commentBuilder.AppendLine("Questions and Answers:");

                for (int i = 0; i < questions.Count; i++)
                {
                    var question = questions[i];
                    var hasAnswer = int.TryParse(form[$"question_{i}"], out int selectedIndex);
                    var selectedAnswer = hasAnswer && selectedIndex >= 0 && selectedIndex < question.Options.Count
                        ? question.Options[selectedIndex]
                        : "(no answer)";
                    var correctAnswer = question.CorrectIndex >= 0 && question.CorrectIndex < question.Options.Count
                        ? question.Options[question.CorrectIndex]
                        : "(invalid correct answer index)";
                    var isCorrect = hasAnswer && selectedIndex == question.CorrectIndex ? "Correct" : "Incorrect";

                    commentBuilder.AppendLine($"{i + 1}. {question.Text}");
                    commentBuilder.AppendLine($"   Learner Answer: {selectedAnswer}");
                    commentBuilder.AppendLine($"   Correct Answer: {correctAnswer}");
                    commentBuilder.AppendLine($"   Result: {isCorrect}");
                }

                attempt.JiraCommentId = await jiraService.PostCommentAsync(storyKey, commentBuilder.ToString());
                await jiraService.TransitionToAsync(storyKey, "In Review");
            }
            else
                logger.LogWarning("Could not find Jira Story for quiz {QuizId} in epic {EpicKey}", quizId, enrollmentRecord.JiraEpicKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating Jira for quiz submission.");
        }

        dbContext.QuizAttempts.Add(attempt);
        await dbContext.SaveChangesAsync();

        return View("Finished", attempt);
    }
}
