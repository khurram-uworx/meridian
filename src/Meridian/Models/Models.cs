using System.ComponentModel.DataAnnotations;
using Uworx.Meridian;
using Uworx.Meridian.CourseSource;

namespace Meridian.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class EnrollmentViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Learner Email")]
    public string LearnerEmail { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Source Type")]
    public CourseSourceType SourceType { get; set; }

    [Required]
    [Display(Name = "Source URI")]
    public string SourceUri { get; set; } = string.Empty;

    [Display(Name = "Sub-path (optional)")]
    public string? SubPath { get; set; }
}

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

public class EnrollmentHistoryViewModel
{
    public int LearnerId { get; set; }
    public string LearnerName { get; set; } = string.Empty;
    public List<EnrollmentHistoryItemViewModel> Enrollments { get; set; } = new();
}

public class EnrollmentHistoryItemViewModel
{
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string JiraEpicKey { get; set; } = string.Empty;
    public double CompletionPct { get; set; }
    public string? ErrorMessage { get; set; }
}
