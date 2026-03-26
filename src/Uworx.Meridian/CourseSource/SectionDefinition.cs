namespace Uworx.Meridian.CourseSource;

public record SectionDefinition(
    string Title,
    int Order,
    string Type,
    int StoryPoints,
    string? QuizId,
    string? DependsOn,
    string BodyMarkdown,
    IEnumerable<QuizQuestion>? QuizQuestions = null
)
{
    public IEnumerable<QuizQuestion> QuizQuestions { get; init; } = QuizQuestions ?? [];
}
