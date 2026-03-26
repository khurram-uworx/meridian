using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Uworx.Meridian.Configuration;
using Uworx.Meridian.Infrastructure;

namespace Meridian.Tests;

[TestFixture, Category(NUnitConstants.TestCatory.Integration)]
public class JiraServiceIntegrationTests
{
    private JiraService _jiraService;
    private JiraOptions _options;

    [SetUp]
    public void SetUp()
    {
        var jiraUrl = Environment.GetEnvironmentVariable("JIRA_URL");
        var jiraUser = Environment.GetEnvironmentVariable("JIRA_USER");
        var jiraToken = Environment.GetEnvironmentVariable("JIRA_TOKEN");

        if (string.IsNullOrEmpty(jiraUrl) || string.IsNullOrEmpty(jiraUser) || string.IsNullOrEmpty(jiraToken))
        {
            Assert.Ignore("Jira integration environment variables are not set.");
        }

        _options = new JiraOptions
        {
            BaseUrl = jiraUrl!,
            UserEmail = jiraUser!,
            ApiToken = jiraToken!,
            ProjectKey = Environment.GetEnvironmentVariable("JIRA_PROJECT") ?? "LEARN",
            StoryPointsField = "customfield_10016"
        };

        _jiraService = new JiraService(Options.Create(_options));
    }

    [Test]
    public async Task CreateEpicAndStoryIntegrationTest()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var epicTitle = $"Meridian Test Epic {timestamp}";
        var epicLabel = "meridian-test";

        // 1. Create Epic
        var epicKey = await _jiraService.CreateEpicAsync(_options.ProjectKey, epicTitle, epicLabel);
        Assert.That(epicKey, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"Created Epic: {epicKey}");

        // 2. Create Story linked to Epic
        var storyTitle = $"Meridian Test Story {timestamp}";
        var storyDescription = "This is a test story created by Meridian integration test.";
        var storyPoints = 3;
        var storyLabel = "lesson";

        var storyKey = await _jiraService.CreateStoryAsync(epicKey, storyTitle, storyDescription, storyPoints, storyLabel);
        Assert.That(storyKey, Is.Not.Null.And.Not.Empty);
        TestContext.WriteLine($"Created Story: {storyKey}");
    }
}
