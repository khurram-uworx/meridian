namespace Uworx.Meridian.Entities;

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
