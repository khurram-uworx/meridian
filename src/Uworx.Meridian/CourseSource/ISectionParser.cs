namespace Uworx.Meridian.CourseSource;

public interface ISectionParser
{
    IEnumerable<SectionDefinition> ParseSections(string coursePath);
}
