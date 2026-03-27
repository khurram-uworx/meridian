using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Microsoft.Extensions.Options;
using Uworx.Meridian.Configuration;

namespace Uworx.Meridian.Infrastructure;

public class JiraService : IJiraService
{
    private readonly IJiraClient _jiraClient;
    private readonly JiraOptions _options;

    public JiraService(IOptions<JiraOptions> options)
    {
        _options = options.Value;
        _jiraClient = JiraClient.Create(new Uri(_options.BaseUrl));
        _jiraClient.SetBasicAuthentication(_options.UserEmail, _options.ApiToken);
    }

    // Constructor for testing with mocked client
    public JiraService(IJiraClient jiraClient, IOptions<JiraOptions> options)
    {
        _jiraClient = jiraClient;
        _options = options.Value;
    }

    public async Task<string> CreateEpicAsync(string projectKey, string title, string label)
    {
        var issue = new Issue
        {
            Fields = new IssueFields
            {
                Project = new Project { Key = projectKey },
                Summary = title,
                IssueType = new IssueType { Name = "Epic" },
                Labels = new List<string> { label }
            }
        };

        // Epic Name is often required. In Dapplo.Jira it might be a custom field.
        // For Epics, "Epic Name" is usually customfield_10011.
        ((IssueV2)issue).AddCustomField("customfield_10011", title);

        var createdIssue = await _jiraClient.Issue.CreateAsync(issue);
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
                Labels = new List<string> { label }
            }
        };

        var issueV2 = (IssueV2)issue;

        // Set Epic Link
        issueV2.AddCustomField("customfield_10014", epicKey); // Epic Link is usually 10014

        // Set story points using custom field
        if (!string.IsNullOrEmpty(_options.StoryPointsField))
        {
            issueV2.AddCustomField(_options.StoryPointsField, storyPoints);
        }

        var createdIssue = await _jiraClient.Issue.CreateAsync(issue);
        return createdIssue.Key;
    }

    public async Task<string> PostCommentAsync(string issueKey, string comment)
    {
        var createdComment = await _jiraClient.Issue.AddCommentAsync(issueKey, comment);
        return createdComment.Id.ToString();
    }

    public async Task TransitionToAsync(string issueKey, string transitionName)
    {
        var transitions = await _jiraClient.Issue.GetTransitionsAsync(issueKey);
        var transition = transitions.FirstOrDefault(t => t.Name.Equals(transitionName, StringComparison.OrdinalIgnoreCase));

        if (transition != null)
        {
            await _jiraClient.Issue.TransitionAsync(issueKey, transition);
        }
    }

    public async Task<string?> FindStoryKeyByLabelAsync(string epicKey, string label)
    {
        var jql = $"'Epic Link' = \"{epicKey}\" AND labels = '{label}'";
        var result = await _jiraClient.Issue.SearchAsync(jql, new Page { StartAt = 0, MaxResults = 1 });
        return result.FirstOrDefault()?.Key;
    }

    public async Task<IEnumerable<JiraStoryStatus>> GetStoriesForEpicAsync(string epicKey)
    {
        var jql = $"'Epic Link' = \"{epicKey}\"";
        var fields = new List<string> { "summary", "status" };
        if (!string.IsNullOrEmpty(_options.StoryPointsField))
        {
            fields.Add(_options.StoryPointsField);
        }

        var result = await _jiraClient.Issue.SearchAsync(jql, new Page { StartAt = 0, MaxResults = 100 }, fields: fields);

        return result.Select(issue => new JiraStoryStatus(
            issue.Key,
            issue.Fields.Summary,
            issue.Fields.Status.Name,
            GetStoryPoints(issue)
        ));
    }

    private int GetStoryPoints(Issue issue)
    {
        if (string.IsNullOrEmpty(_options.StoryPointsField)) return 0;

        var value = ((IssueV2)issue).GetCustomField(_options.StoryPointsField);
        if (value == null) return 0;

        if (int.TryParse(value.ToString(), out int points))
        {
            return points;
        }

        return 0;
    }
}
