using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Uworx.Meridian.Configuration;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class ConfigurationTests
{
    [Test]
    public void JiraOptions_ShouldBindCorrectlly()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?> {
            {"Jira:BaseUrl", "https://test.atlassian.net"},
            {"Jira:ApiToken", "test-token"},
            {"Jira:UserEmail", "test@example.com"},
            {"Jira:ProjectKey", "TEST"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.Configure<JiraOptions>(configuration.GetSection(JiraOptions.Jira));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<JiraOptions>>().Value;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.BaseUrl, Is.EqualTo("https://test.atlassian.net"));
            Assert.That(options.ApiToken, Is.EqualTo("test-token"));
            Assert.That(options.UserEmail, Is.EqualTo("test@example.com"));
            Assert.That(options.ProjectKey, Is.EqualTo("TEST"));
        });
    }
}
