using System.Text.RegularExpressions;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Exceptions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Uworx.Meridian.Infrastructure.CourseSource;

public class CourseConfigParser : ICourseConfigParser
{
    class CourseConfigDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? JiraProject { get; set; }
        public string? EpicLabel { get; set; }
        public string? EpicDescription { get; set; }
    }

    static (string metadata, string? introText) splitMetadataAndIntroText(string yamlContent)
    {
        var normalized = yamlContent.Replace("\r\n", "\n");

        // Supported formats:
        // 1) Plain YAML only.
        // 2) Plain YAML, then a separator line `---`, then free-form intro text.
        // 3) Frontmatter style: `---` + YAML + `---` + free-form intro text.

        List<string> startsWith = ["---\n", "\n---\n"];

        var matched = startsWith.FirstOrDefault(s => normalized.StartsWith(s, StringComparison.Ordinal));

        if (matched is not null)
        {
            var endFrontmatter = normalized.IndexOf(matched, matched.Length, StringComparison.Ordinal);
            if (endFrontmatter >= 0)
            {
                var metadata = normalized.Substring(matched.Length, endFrontmatter - matched.Length);
                var introText = normalized.Substring(endFrontmatter + matched.Length).Trim();
                return (metadata, string.IsNullOrWhiteSpace(introText) ? null : introText);
            }
        }

        var separatorMatch = Regex.Match(normalized, @"\n---\n", RegexOptions.Multiline);
        if (separatorMatch.Success)
        {
            var metadata = normalized.Substring(0, separatorMatch.Index);
            var introText = normalized.Substring(separatorMatch.Index + separatorMatch.Length).Trim();
            return (metadata, string.IsNullOrWhiteSpace(introText) ? null : introText);
        }

        return (normalized, null);
    }

    void validate(CourseConfigDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            throw new InvalidCourseException("Required field 'id' is missing in course.yaml");
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new InvalidCourseException("Required field 'title' is missing in course.yaml");
    }

    public CourseConfig Parse(string coursePath)
    {
        var yamlPath = Path.Combine(coursePath, "course.yaml");
        if (!File.Exists(yamlPath))
            throw new InvalidCourseException($"Course configuration file not found: course.yaml in {coursePath}");

        string yamlContent;
        try
        {
            yamlContent = File.ReadAllText(yamlPath);
        }
        catch (Exception ex)
        {
            throw new InvalidCourseException($"Failed to read course.yaml: {ex.Message}", ex);
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var (metadataYaml, introTextFromBody) = splitMetadataAndIntroText(yamlContent);

        CourseConfigDto? dto;
        try
        {
            dto = deserializer.Deserialize<CourseConfigDto>(metadataYaml);
        }
        catch (Exception ex)
        {
            throw new InvalidCourseException($"Error parsing course.yaml: {ex.Message}", ex);
        }

        if (dto == null)
            throw new InvalidCourseException("course.yaml is empty or invalid.");

        validate(dto);

        var epicDescription = string.IsNullOrWhiteSpace(dto.EpicDescription)
            ? introTextFromBody
            : dto.EpicDescription.Trim();

        return new CourseConfig(
            dto.Id!,
            dto.Title!,
            dto.Version,
            dto.Author,
            dto.JiraProject!,
            dto.EpicLabel,
            epicDescription
        );
    }
}
