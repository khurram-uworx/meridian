using NUnit.Framework;
using Uworx.Meridian.Infrastructure.CourseSource;

namespace Meridian.Tests;

[TestFixture]
public class SectionParserTests
{
    private string _tempCoursePath;
    private SectionParser _parser;

    [SetUp]
    public void Setup()
    {
        _tempCoursePath = Path.Combine(Path.GetTempPath(), "meridian_section_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempCoursePath);
        _parser = new SectionParser();
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
    public void ParseSections_FullFrontmatter_PopulatesAllFields()
    {
        // Arrange
        var content = @"---
title: ""Introduction to CI/CD""
order: 3
type: lesson
story_points: 3
quiz: intro-cicd-q1
depends_on: 02-prerequisites
---

## What is CI/CD?
Content here.";
        File.WriteAllText(Path.Combine(_tempCoursePath, "01-intro.md"), content);

        // Act
        var result = _parser.ParseSections(_tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var section = result[0];
        Assert.That(section.Title, Is.EqualTo("Introduction to CI/CD"));
        Assert.That(section.Order, Is.EqualTo(3));
        Assert.That(section.Type, Is.EqualTo("lesson"));
        Assert.That(section.StoryPoints, Is.EqualTo(3));
        Assert.That(section.QuizId, Is.EqualTo("intro-cicd-q1"));
        Assert.That(section.DependsOn, Is.EqualTo("02-prerequisites"));
        Assert.That(section.BodyMarkdown, Is.EqualTo("## What is CI/CD?\nContent here."));
    }

    [Test]
    public void ParseSections_NoFrontmatter_UsesFilenameFallbacks()
    {
        // Arrange
        var content = @"## Just Markdown Content";
        File.WriteAllText(Path.Combine(_tempCoursePath, "05-setup_guide.md"), content);

        // Act
        var result = _parser.ParseSections(_tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var section = result[0];
        Assert.That(section.Title, Is.EqualTo("setup guide"));
        Assert.That(section.Order, Is.EqualTo(5));
        Assert.That(section.Type, Is.EqualTo("lesson"));
        Assert.That(section.BodyMarkdown, Is.EqualTo("## Just Markdown Content"));
    }

    [Test]
    public void ParseSections_MultipleFiles_ReturnsInOrder()
    {
        // Arrange
        var file1 = @"---
order: 2
---
Second";
        var file2 = @"---
order: 1
---
First";
        File.WriteAllText(Path.Combine(_tempCoursePath, "a.md"), file1);
        File.WriteAllText(Path.Combine(_tempCoursePath, "b.md"), file2);

        // Act
        var result = _parser.ParseSections(_tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].BodyMarkdown, Is.EqualTo("First"));
        Assert.That(result[1].BodyMarkdown, Is.EqualTo("Second"));
    }

    [Test]
    public void ParseSections_BodyMarkdown_ExtractsEverythingAfterClosingDashes()
    {
        // Arrange
        var content = @"---
title: Test
---
This is the body.
It has multiple lines.
---
Even more dashes.";
        File.WriteAllText(Path.Combine(_tempCoursePath, "test.md"), content);

        // Act
        var result = _parser.ParseSections(_tempCoursePath).ToList();

        // Assert
        Assert.That(result[0].BodyMarkdown, Is.EqualTo("This is the body.\nIt has multiple lines.\n---\nEven more dashes."));
    }
}
