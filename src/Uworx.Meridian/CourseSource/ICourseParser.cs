namespace Uworx.Meridian.CourseSource;

public enum CourseSourceType
{
    Git,
    LocalFolder
}

public record CourseSourceLocator(CourseSourceType SourceType, string Uri, string? SubPath = null);
public record ParsedCourse(CourseConfig Config, IReadOnlyList<SectionDefinition> Sections, string? SourceRevision);

public interface ICourseParser
{
    Task<ParsedCourse> ParseAsync(CourseSourceLocator locator);
}
