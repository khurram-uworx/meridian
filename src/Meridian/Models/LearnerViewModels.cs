using Uworx.Meridian;

namespace Meridian.Models;

public class LearnerProgressViewModel
{
    public int LearnerId { get; set; }
    public string LearnerName { get; set; } = string.Empty;
    public List<CourseProgressViewModel> Courses { get; set; } = new();
}

public class CourseProgressViewModel
{
    public string CourseTitle { get; set; } = string.Empty;
    public string EpicKey { get; set; } = string.Empty;
    public double CompletionPct { get; set; }
    public List<JiraStoryStatus> Stories { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
