using System.ComponentModel.DataAnnotations;
using Uworx.Meridian.CourseSource;

namespace Meridian.Models;

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
