namespace Uworx.Meridian.Entities;

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
