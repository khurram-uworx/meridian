using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;

namespace Uworx.Meridian.Infrastructure;

public class EnrollmentService : IEnrollmentService
{
    private readonly MeridianDbContext _dbContext;
    private readonly ICourseParser _courseParser;
    private readonly IJiraService _jiraService;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(
        MeridianDbContext dbContext,
        ICourseParser courseParser,
        IJiraService jiraService,
        ILogger<EnrollmentService> logger)
    {
        _dbContext = dbContext;
        _courseParser = courseParser;
        _jiraService = jiraService;
        _logger = logger;
    }

    public async Task<Enrollment> EnrollAsync(string learnerEmail, CourseSourceLocator source)
    {
        // 1. Parse course
        var parsedCourse = await _courseParser.ParseAsync(source);

        // 2. Upsert Learner
        var learner = await _dbContext.Learners
            .FirstOrDefaultAsync(l => l.Email == learnerEmail);

        if (learner == null)
        {
            learner = new Learner
            {
                Email = learnerEmail,
                Name = learnerEmail.Split('@')[0], // Placeholder
                JiraAccountId = learnerEmail // Placeholder
            };
            _dbContext.Learners.Add(learner);
        }

        // 3. Upsert Course
        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(c => c.SourceType == source.SourceType.ToString() &&
                                     c.SourceLocator == source.Uri &&
                                     c.CoursePath == (source.SubPath ?? string.Empty));

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
            _dbContext.Courses.Add(course);
        }
        else
        {
            course.CourseYamlSnapshot = courseYamlSnapshot;
        }

        await _dbContext.SaveChangesAsync();

        // 4. Create Jira Epic
        string epicKey;
        try
        {
            epicKey = await _jiraService.CreateEpicAsync(
                parsedCourse.Config.JiraProject,
                parsedCourse.Config.Title,
                parsedCourse.Config.EpicLabel ?? "meridian");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Jira Epic for {CourseTitle}", parsedCourse.Config.Title);
            throw;
        }

        // 5. Create Jira Stories
        try
        {
            foreach (var section in parsedCourse.Sections)
            {
                await _jiraService.CreateStoryAsync(
                    epicKey,
                    section.Title,
                    section.BodyMarkdown,
                    section.StoryPoints,
                    section.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Jira Stories for Epic {EpicKey}. Cleanup required.", epicKey);
            // We don't delete the epic as per requirement (PoC), but we must ensure DB is not persisted.
            throw;
        }

        // 6. Create Enrollment record
        var enrollment = new Enrollment
        {
            LearnerId = learner.Id,
            CourseId = course.Id,
            JiraEpicKey = epicKey,
            EnrolledAt = DateTime.UtcNow,
            SourceRevision = parsedCourse.SourceRevision
        };

        _dbContext.Enrollments.Add(enrollment);
        await _dbContext.SaveChangesAsync();

        return enrollment;
    }
}
