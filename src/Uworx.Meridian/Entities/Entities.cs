namespace Uworx.Meridian.Entities;

public class Course
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceLocator { get; set; } = string.Empty;
    public string CoursePath { get; set; } = string.Empty;
    public string CourseYamlSnapshot { get; set; } = string.Empty;
}

public class Learner
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JiraAccountId { get; set; } = string.Empty;
}

public class Enrollment
{
    public int Id { get; set; }
    public int LearnerId { get; set; }
    public int CourseId { get; set; }
    public string JiraEpicKey { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string? SourceRevision { get; set; }

    public Learner Learner { get; set; } = null!;
    public Course Course { get; set; } = null!;
}

public class QuizAttempt
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public string QuizId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public DateTime AttemptedAt { get; set; }
    public string? JiraCommentId { get; set; }

    public Enrollment Enrollment { get; set; } = null!;
}

public class EnrollmentOperation
{
    public Guid Id { get; set; }
    public string LearnerEmail { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public string? SubPath { get; set; }
    public string Status { get; set; } = "queued";
    public string CurrentStage { get; set; } = "queued";
    public string CurrentMessage { get; set; } = string.Empty;
    public bool IsWarning { get; set; }
    public int? ProgressPercent { get; set; }
    public int? EnrollmentId { get; set; }
    public string? JiraEpicKey { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<EnrollmentOperationEvent> Events { get; set; } = new List<EnrollmentOperationEvent>();
}

public class EnrollmentOperationEvent
{
    public int Id { get; set; }
    public Guid EnrollmentOperationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsWarning { get; set; }
    public int? ProgressPercent { get; set; }

    public EnrollmentOperation EnrollmentOperation { get; set; } = null!;
}
