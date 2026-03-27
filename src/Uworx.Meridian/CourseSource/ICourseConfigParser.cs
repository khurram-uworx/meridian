namespace Uworx.Meridian.CourseSource;

public record CourseConfig(
    string Id,
    string Title,
    string? Version,
    string? Author,
    string? JiraProject,
    string? EpicLabel,
    string? EpicDescription
);

public interface ICourseConfigParser
{
    CourseConfig Parse(string coursePath);
}
