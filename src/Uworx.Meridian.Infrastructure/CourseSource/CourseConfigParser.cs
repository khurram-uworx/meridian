using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Exceptions;

namespace Uworx.Meridian.Infrastructure.CourseSource;

public class CourseConfigParser : ICourseConfigParser
{
    public CourseConfig Parse(string coursePath)
    {
        var yamlPath = Path.Combine(coursePath, "course.yaml");
        if (!File.Exists(yamlPath))
        {
            throw new InvalidCourseException($"Course configuration file not found: course.yaml in {coursePath}");
        }

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

        CourseConfigDto? dto;
        try
        {
            dto = deserializer.Deserialize<CourseConfigDto>(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidCourseException($"Error parsing course.yaml: {ex.Message}", ex);
        }

        if (dto == null)
        {
            throw new InvalidCourseException("course.yaml is empty or invalid.");
        }

        Validate(dto);

        return new CourseConfig(
            dto.Id!,
            dto.Title!,
            dto.Version,
            dto.Author,
            dto.JiraProject!,
            dto.EpicLabel
        );
    }

    private void Validate(CourseConfigDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
        {
            throw new InvalidCourseException("Required field 'id' is missing in course.yaml");
        }
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidCourseException("Required field 'title' is missing in course.yaml");
        }
        if (string.IsNullOrWhiteSpace(dto.JiraProject))
        {
            throw new InvalidCourseException("Required field 'jira_project' is missing in course.yaml");
        }
    }

    private class CourseConfigDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? JiraProject { get; set; }
        public string? EpicLabel { get; set; }
    }
}
