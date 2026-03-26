namespace Uworx.Meridian.CourseSource;

public interface ICourseSourceResolver
{
    Task<CourseSourceResult> ResolveAsync(CourseSourceLocator locator);
}
