namespace Uworx.Meridian.Configuration;

public class JiraOptions
{
    public const string Jira = "Jira";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
    public string StoryPointsField { get; set; } = "customfield_10016";
}
