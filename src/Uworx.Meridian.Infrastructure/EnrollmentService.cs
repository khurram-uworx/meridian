using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;

namespace Uworx.Meridian.Infrastructure;

public class EnrollmentService : IEnrollmentService
{
    readonly IOptions<JiraOptions> options;
    readonly MeridianDbContext dbContext;
    readonly ICourseParser courseParser;
    readonly IJiraService jiraService;
    readonly ILogger<EnrollmentService> logger;

    public EnrollmentService(
        IOptions<JiraOptions> options,
        MeridianDbContext dbContext,
        ICourseParser courseParser,
        IJiraService jiraService,
        ILogger<EnrollmentService> logger)
    {
        this.options = options;
        this.dbContext = dbContext;
        this.courseParser = courseParser;
        this.jiraService = jiraService;
        this.logger = logger;
    }

    public async Task<Enrollment> EnrollAsync(string learnerEmail, CourseSourceLocator source)
    {
        // 1. Parse course
        var parsedCourse = await courseParser.ParseAsync(source);

        // 2. Upsert Learner
        var learner = await dbContext.Learners
            .FirstOrDefaultAsync(l => l.Email == learnerEmail);

        if (learner == null)
        {
            learner = new Learner
            {
                Email = learnerEmail,
                Name = learnerEmail.Split('@')[0], // Placeholder
                JiraAccountId = learnerEmail // Placeholder 
            };
            dbContext.Learners.Add(learner);
        }

        // 3. Upsert Course
        var course = await dbContext.Courses.FirstOrDefaultAsync(
            c => c.SourceType == source.SourceType.ToString()
            && c.SourceLocator == source.Uri
            && c.CoursePath == (source.SubPath ?? string.Empty));

        var courseYamlSnapshot = JsonSerializer.Serialize(parsedCourse.Config);

        if (course == null)
        {
            course = new Course
            {
                SourceType = source.SourceType.ToString(),
                SourceLocator = source.Uri,
                CoursePath = source.SubPath ?? string.Empty,
                CourseYamlSnapshot = courseYamlSnapshot
            };
            dbContext.Courses.Add(course);
        }
        else
            course.CourseYamlSnapshot = courseYamlSnapshot;

        await dbContext.SaveChangesAsync();

        // 4. Create Jira Epic
        string epicKey;
        try
        {
            epicKey = await jiraService.CreateEpicAsync(
                parsedCourse.Config.JiraProject ?? this.options.Value.ProjectKey,
                parsedCourse.Config.Title,
                parsedCourse.Config.EpicLabel ?? "meridian",
                parsedCourse.Config.EpicDescription);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Jira Epic for {CourseTitle}", parsedCourse.Config.Title);
            throw;
        }

        // 5. Create Enrollment record early to get the ID for quiz links
        var enrollment = new Enrollment
        {
            LearnerId = learner.Id,
            CourseId = course.Id,
            JiraEpicKey = epicKey,
            EnrolledAt = DateTime.UtcNow,
            SourceRevision = parsedCourse.SourceRevision
        };

        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync();

        // 6. Create Jira Stories
        try
        {
            foreach (var section in parsedCourse.Sections)
            {
                var description = section.BodyMarkdown;
                var label = string.IsNullOrWhiteSpace(section.QuizId) ? "section" : section.QuizId;

                var storyKey = await jiraService.CreateStoryAsync(
                    epicKey,
                    section.Title,
                    description,
                    section.StoryPoints,
                    label);

                if (!string.IsNullOrWhiteSpace(section.QuizId))
                {
                    // For PoC we use a placeholder host, in real world this should be configurable
                    var quizLinkComment = $"Quiz available: [Take the Quiz](http://localhost:5000/quiz/{section.QuizId}?enrollment={enrollment.Id})";
                    await jiraService.PostCommentAsync(storyKey, quizLinkComment);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Jira Stories for Epic {EpicKey}. Cleanup required.", epicKey);

            // Cleanup enrollment record if stories failed to create
            dbContext.Enrollments.Remove(enrollment);
            await dbContext.SaveChangesAsync();

            // We don't delete the epic as per requirement (PoC), but we must ensure DB is not persisted.
            throw;
        }

        return enrollment;
    }

    public async Task<IEnumerable<Enrollment>> GetEnrollmentsByLearnerIdAsync(int learnerId)
    {
        return await dbContext.Enrollments
            .Include(e => e.Course)
            .Where(e => e.LearnerId == learnerId)
            .ToListAsync();
    }
}
