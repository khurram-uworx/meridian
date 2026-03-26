namespace Uworx.Meridian.CourseSource;

public record CourseConfig(
    string Id,
    string Title,
    string? Version,
    string? Author,
    string JiraProject,
    string? EpicLabel
);
