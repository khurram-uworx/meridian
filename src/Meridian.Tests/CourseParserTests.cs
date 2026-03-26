using NUnit.Framework;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Infrastructure.CourseSource;

namespace Meridian.Tests;

[TestFixture]
public class CourseParserTests
{
    private string _tempCoursePath;
    private CourseParser _parser;

    [SetUp]
    public void Setup()
    {
        _tempCoursePath = Path.Combine(Path.GetTempPath(), "meridian_parser_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempCoursePath);

        var sourceResolver = new CourseSourceResolver();
        var configParser = new CourseConfigParser();
        var sectionParser = new SectionParser();
        _parser = new CourseParser(sourceResolver, configParser, sectionParser);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempCoursePath))
        {
            Directory.Delete(_tempCoursePath, true);
        }
    }

    [Test]
    public async Task ParseAsync_LocalFolder_ReturnsFullyPopulatedParsedCourse()
    {
        // Arrange
        var courseYaml = @"
id: X101
title: ""Test Course""
jira_project: LEARN
";
        File.WriteAllText(Path.Combine(_tempCoursePath, "course.yaml"), courseYaml);

        var section1 = @"---
title: ""Section 1""
order: 1
---
Content 1";
        var section2 = @"---
title: ""Section 2""
order: 2
---
Content 2";
        File.WriteAllText(Path.Combine(_tempCoursePath, "01-intro.md"), section1);
        File.WriteAllText(Path.Combine(_tempCoursePath, "02-setup.md"), section2);

        var locator = new CourseSourceLocator(CourseSourceType.LocalFolder, _tempCoursePath);

        // Act
        var result = await _parser.ParseAsync(locator);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Config.Id, Is.EqualTo("X101"));
        Assert.That(result.Config.Title, Is.EqualTo("Test Course"));
        Assert.That(result.Sections.Count, Is.EqualTo(2));
        Assert.That(result.Sections[0].Title, Is.EqualTo("Section 1"));
        Assert.That(result.Sections[1].Title, Is.EqualTo("Section 2"));
        Assert.That(result.SourceRevision, Is.Null);
    }
}
