namespace Uworx.Meridian.CourseSource;

public record CourseSourceResult(string FolderPath, string? SourceRevision);

public interface ICourseSourceResolver
{
    Task<CourseSourceResult> ResolveAsync(CourseSourceLocator locator);
}
