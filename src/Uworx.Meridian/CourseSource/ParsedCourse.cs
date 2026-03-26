using System.Collections.Generic;

namespace Uworx.Meridian.CourseSource;

public record ParsedCourse(
    CourseConfig Config,
    IReadOnlyList<SectionDefinition> Sections,
    string? SourceRevision
);
