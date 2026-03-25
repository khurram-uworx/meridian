namespace Uworx.Meridian.Entities;

public class Course
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceLocator { get; set; } = string.Empty;
    public string CoursePath { get; set; } = string.Empty;
    public string CourseYamlSnapshot { get; set; } = string.Empty;
}
