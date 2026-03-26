using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Uworx.Meridian.CourseSource;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Uworx.Meridian.Infrastructure.CourseSource;

public class SectionParser : ISectionParser
{
    private readonly IDeserializer _deserializer;
    private readonly ILogger<SectionParser> _logger;

    public SectionParser(ILogger<SectionParser> logger)
    {
        _logger = logger;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public IEnumerable<SectionDefinition> ParseSections(string coursePath)
    {
        var mdFiles = Directory.GetFiles(coursePath, "*.md");
        var sections = new List<SectionDefinition>();

        foreach (var file in mdFiles)
        {
            var fileName = Path.GetFileName(file);
            var content = File.ReadAllText(file);

            var (frontmatter, body) = ExtractFrontmatter(content);

            SectionFrontmatterDto? dto = null;
            if (!string.IsNullOrWhiteSpace(frontmatter))
            {
                try
                {
                    dto = _deserializer.Deserialize<SectionFrontmatterDto>(frontmatter);
                }
                catch
                {
                    // If parsing fails, we treat it as no frontmatter
                }
            }

            var title = dto?.Title ?? InferTitleFromFileName(fileName);
            var order = dto?.Order ?? InferOrderFromFileName(fileName);
            var type = dto?.Type ?? "lesson";
            var storyPoints = dto?.StoryPoints ?? 0;
            var quizId = dto?.Quiz;
            var dependsOn = dto?.DependsOn;
            var quizQuestions = dto?.QuizQuestions?.Select(q => new QuizQuestion(
                q.Text ?? string.Empty,
                q.Options ?? new List<string>(),
                q.CorrectIndex ?? 0
            )).ToList() ?? new List<QuizQuestion>();

            if (type == "quiz" && !quizQuestions.Any())
            {
                _logger.LogWarning("Section {FileName} has type 'quiz' but no quiz_questions defined. Treating as 'lesson'.", fileName);
                type = "lesson";
            }

            sections.Add(new SectionDefinition(
                title,
                order,
                type,
                storyPoints,
                quizId,
                dependsOn,
                body.Trim(),
                quizQuestions
            ));
        }

        return sections.OrderBy(s => s.Order).ToList();
    }

    private (string frontmatter, string body) ExtractFrontmatter(string content)
    {
        var regex = new Regex(@"^---\s*\n(.*?)\n---\s*\n(.*)$", RegexOptions.Singleline);
        var match = regex.Match(content);

        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value);
        }

        return (string.Empty, content);
    }

    private string InferTitleFromFileName(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        // Remove numeric prefix like 00-, 01-
        var title = Regex.Replace(nameWithoutExtension, @"^\d+-", "");
        // Replace dashes/underscores with spaces and capitalize
        title = title.Replace('-', ' ').Replace('_', ' ');
        return title.Trim();
    }

    private int InferOrderFromFileName(string fileName)
    {
        var match = Regex.Match(fileName, @"^(\d+)-");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var order))
        {
            return order;
        }
        return 999; // Default for files without numeric prefix
    }

    private class SectionFrontmatterDto
    {
        public string? Title { get; set; }
        public int? Order { get; set; }
        public string? Type { get; set; }
        public int? StoryPoints { get; set; }
        public string? Quiz { get; set; }
        public string? DependsOn { get; set; }
        public List<QuizQuestionDto>? QuizQuestions { get; set; }
    }

    private class QuizQuestionDto
    {
        public string? Text { get; set; }
        public List<string>? Options { get; set; }
        public int? CorrectIndex { get; set; }
    }
}
