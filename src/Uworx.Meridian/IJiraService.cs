namespace Uworx.Meridian;

public interface IJiraService
{
    /// <summary>
    /// Creates an Epic in Jira.
    /// </summary>
    /// <param name="projectKey">The Jira project key (e.g., "LEARN").</param>
    /// <param name="title">The title of the Epic.</param>
    /// <param name="label">A label to apply to the Epic.</param>
    /// <returns>The key of the created Epic (e.g., "LEARN-42").</returns>
    Task<string> CreateEpicAsync(string projectKey, string title, string label);

    /// <summary>
    /// Creates a Story in Jira and links it to an Epic.
    /// </summary>
    /// <param name="epicKey">The key of the parent Epic.</param>
    /// <param name="title">The title of the Story.</param>
    /// <param name="description">The description of the Story (Markdown or plain text).</param>
    /// <param name="storyPoints">The story points to assign.</param>
    /// <param name="label">A label to apply (e.g., section type like lesson, quiz, lab).</param>
    /// <returns>The key of the created Story.</returns>
    Task<string> CreateStoryAsync(string epicKey, string title, string description, int storyPoints, string label);
}
