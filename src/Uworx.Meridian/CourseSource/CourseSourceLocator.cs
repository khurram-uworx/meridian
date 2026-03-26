namespace Uworx.Meridian.CourseSource;

public record CourseSourceLocator(CourseSourceType SourceType, string Uri, string? SubPath = null);
