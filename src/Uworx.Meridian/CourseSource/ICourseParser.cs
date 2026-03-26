using System.Threading.Tasks;

namespace Uworx.Meridian.CourseSource;

public interface ICourseParser
{
    Task<ParsedCourse> ParseAsync(CourseSourceLocator locator);
}
