using Uworx.Meridian.CourseSource;

namespace Uworx.Meridian.Infrastructure.CourseSource;

public class CourseParser : ICourseParser
{
    readonly ICourseSourceResolver sourceResolver;
    readonly ICourseConfigParser configParser;
    readonly ISectionParser sectionParser;

    public CourseParser(
        ICourseSourceResolver sourceResolver,
        ICourseConfigParser configParser,
        ISectionParser sectionParser)
    {
        this.sourceResolver = sourceResolver;
        this.configParser = configParser;
        this.sectionParser = sectionParser;
    }

    public async Task<ParsedCourse> ParseAsync(CourseSourceLocator locator)
    {
        var sourceResult = await sourceResolver.ResolveAsync(locator);

        var config = configParser.Parse(sourceResult.FolderPath);
        var sections = sectionParser.ParseSections(sourceResult.FolderPath).ToList();

        return new ParsedCourse(config, sections, sourceResult.SourceRevision);
    }
}
