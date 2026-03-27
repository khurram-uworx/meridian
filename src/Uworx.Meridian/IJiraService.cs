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

    /// <summary>
    /// Posts a comment to a Jira issue.
    /// </summary>
    /// <param name="issueKey">The key of the issue.</param>
    /// <param name="comment">The comment text.</param>
    /// <returns>The ID of the created comment.</returns>
    Task<string> PostCommentAsync(string issueKey, string comment);

    /// <summary>
    /// Transitions a Jira issue to a new status.
    /// </summary>
    /// <param name="issueKey">The key of the issue.</param>
    /// <param name="transitionName">The name of the transition (e.g., "In Review", "Done").</param>
    Task TransitionToAsync(string issueKey, string transitionName);

    /// <summary>
    /// Finds a story key in an epic by a specific label.
    /// </summary>
    /// <param name="epicKey">The key of the parent epic.</param>
    /// <param name="label">The label to search for.</param>
    /// <returns>The story key, or null if not found.</returns>
    Task<string?> FindStoryKeyByLabelAsync(string epicKey, string label);

    /// <summary>
    /// Gets all stories for a specific Epic.
    /// </summary>
    /// <param name="epicKey">The key of the parent Epic.</param>
    /// <returns>A list of story statuses.</returns>
    Task<IEnumerable<JiraStoryStatus>> GetStoriesForEpicAsync(string epicKey);
}
