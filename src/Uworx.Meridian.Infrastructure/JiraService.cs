using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Microsoft.Extensions.Options;
using Uworx.Meridian.Configuration;

namespace Uworx.Meridian.Infrastructure;

public class JiraService : IJiraService
{
    readonly IJiraClient jiraClient;
    readonly JiraOptions options;

    int getStoryPoints(Issue issue)
    {
        if (string.IsNullOrEmpty(options.StoryPointsField)) return 0;

        var value = ((IssueV2)issue).GetCustomField(options.StoryPointsField);
        if (value == null) return 0;

        if (int.TryParse(value.ToString(), out int points))
            return points;

        return 0;
    }

    public JiraService(IOptions<JiraOptions> options)
    {
        this.options = options.Value;
        jiraClient = JiraClient.Create(new Uri(this.options.BaseUrl));
        jiraClient.SetBasicAuthentication(this.options.UserEmail, this.options.ApiToken);
    }

    // Constructor for testing with mocked client
    public JiraService(IJiraClient jiraClient, IOptions<JiraOptions> options)
    {
        this.jiraClient = jiraClient;
        this.options = options.Value;
    }

    public async Task<string> CreateEpicAsync(string projectKey, string title, string label, string? description = null)
    {
        var issue = new Issue
        {
            Fields = new IssueFields
            {
                Project = new Project { Key = projectKey },
                Summary = title,
                Description = string.IsNullOrWhiteSpace(description) ? null : (AdfDocument)description,
                IssueType = new IssueType { Name = "Epic" },
                Labels = new List<string> { label }
            }
        };

        var createdIssue = await jiraClient.Issue.CreateAsync(issue);
        return createdIssue.Key;
    }

    public async Task<string> CreateStoryAsync(string epicKey, string title, string description, int storyPoints, string label)
    {
        var issue = new Issue
        {
            Fields = new IssueFields
            {
                Project = new Project { Key = epicKey.Split('-')[0] },
                Summary = title,
                Description = (AdfDocument)description,
                IssueType = new IssueType { Name = "Story" },
                Parent = new IssueWithFields<IssueFields> { Key = epicKey },
                Labels = new List<string> { label }
            }
        };

        var issueV2 = (IssueV2)issue;

        // Set story points using custom field
        if (!string.IsNullOrEmpty(options.StoryPointsField))
            issueV2.AddCustomField(options.StoryPointsField, storyPoints);

        var createdIssue = await jiraClient.Issue.CreateAsync(issue);
        return createdIssue.Key;
    }

    public async Task<string> PostCommentAsync(string issueKey, string comment)
    {
        var createdComment = await jiraClient.Issue.AddCommentAsync(issueKey, comment);
        return createdComment.Id.ToString();
    }

    public async Task TransitionToAsync(string issueKey, string transitionName)
    {
        var transitions = await jiraClient.Issue.GetTransitionsAsync(issueKey);
        var transition = transitions.FirstOrDefault(t => t.Name.Equals(transitionName, StringComparison.OrdinalIgnoreCase));

        if (transition != null)
            await jiraClient.Issue.TransitionAsync(issueKey, transition);
    }

    public async Task<string?> FindStoryKeyByLabelAsync(string epicKey, string label)
    {
        var jql = $"'Epic Link' = \"{epicKey}\" AND labels = '{label}'";
        var result = await jiraClient.Issue.SearchAsync(jql, new Page { StartAt = 0, MaxResults = 1 });
        return result.FirstOrDefault()?.Key;
    }

    public async Task<IEnumerable<JiraStoryStatus>> GetStoriesForEpicAsync(string epicKey)
    {
        var jql = $"'Epic Link' = \"{epicKey}\"";
        var fields = new List<string> { "summary", "status" };
        if (!string.IsNullOrEmpty(options.StoryPointsField))
            fields.Add(options.StoryPointsField);

        var result = await jiraClient.Issue.SearchAsync(jql, new Page { StartAt = 0, MaxResults = 100 }, fields: fields);

        return result.Select(issue => new JiraStoryStatus(
            issue.Key,
            issue.Fields.Summary,
            issue.Fields.Status.Name,
            getStoryPoints(issue)
        ));
    }
}
