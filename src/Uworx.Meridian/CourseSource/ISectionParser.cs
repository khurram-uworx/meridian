namespace Uworx.Meridian.CourseSource;

public record QuizQuestion(string Text, List<string> Options, int CorrectIndex);

public record SectionDefinition(string Title, int Order, int StoryPoints,
    string? QuizId, string? DependsOn,
    string BodyMarkdown,
    IEnumerable<QuizQuestion>? QuizQuestions = null
)
{
    public IEnumerable<QuizQuestion> QuizQuestions { get; init; } = QuizQuestions ?? [];
}

public interface ISectionParser
{
    IEnumerable<SectionDefinition> ParseSections(string coursePath);
}
