using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uworx.Meridian.CourseSource;

namespace Uworx.Meridian.Infrastructure.CourseSource;

public class CourseParser : ICourseParser
{
    private readonly ICourseSourceResolver _sourceResolver;
    private readonly ICourseConfigParser _configParser;
    private readonly ISectionParser _sectionParser;

    public CourseParser(
        ICourseSourceResolver sourceResolver,
        ICourseConfigParser configParser,
        ISectionParser sectionParser)
    {
        _sourceResolver = sourceResolver;
        _configParser = configParser;
        _sectionParser = sectionParser;
    }

    public async Task<ParsedCourse> ParseAsync(CourseSourceLocator locator)
    {
        var sourceResult = await _sourceResolver.ResolveAsync(locator);

        var config = _configParser.Parse(sourceResult.FolderPath);
        var sections = _sectionParser.ParseSections(sourceResult.FolderPath).ToList();

        return new ParsedCourse(config, sections, sourceResult.SourceRevision);
    }
}
