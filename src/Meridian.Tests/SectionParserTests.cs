using NUnit.Framework;
using Uworx.Meridian.Infrastructure.CourseSource;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class SectionParserTests
{
    string? tempCoursePath;
    SectionParser? parser;

    [SetUp]
    public void Setup()
    {
        tempCoursePath = Path.Combine(Path.GetTempPath(), "meridian_section_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempCoursePath);
        parser = new SectionParser();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempCoursePath))
            Directory.Delete(tempCoursePath, true);
    }

    [Test]
    public void ParseSections_FullFrontmatter_PopulatesAllFields()
    {
        // Arrange
        var content = @"---
title: ""Introduction to CI/CD""
order: 3
story_points: 3
quiz: intro-cicd-q1
depends_on: 02-prerequisites
---

## What is CI/CD?
Content here.";
        File.WriteAllText(Path.Combine(tempCoursePath, "01-intro.md"), content);

        // Act
        var result = parser.ParseSections(tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var section = result[0];
        Assert.That(section.Title, Is.EqualTo("Introduction to CI/CD"));
        Assert.That(section.Order, Is.EqualTo(3));
        Assert.That(section.StoryPoints, Is.EqualTo(3));
        Assert.That(section.QuizId, Is.EqualTo("intro-cicd-q1"));
        Assert.That(section.DependsOn, Is.EqualTo("02-prerequisites"));
        Assert.That(section.BodyMarkdown, Is.EqualTo(string.Format("## What is CI/CD?{0}Content here.", Environment.NewLine)));
    }

    [Test]
    public void ParseSections_NoFrontmatter_UsesFilenameFallbacks()
    {
        // Arrange
        var content = @"## Just Markdown Content";
        File.WriteAllText(Path.Combine(tempCoursePath, "05-setup_guide.md"), content);

        // Act
        var result = parser.ParseSections(tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var section = result[0];
        Assert.That(section.Title, Is.EqualTo("setup guide"));
        Assert.That(section.Order, Is.EqualTo(5));
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
        File.WriteAllText(Path.Combine(tempCoursePath, "a.md"), file1);
        File.WriteAllText(Path.Combine(tempCoursePath, "b.md"), file2);

        // Act
        var result = parser.ParseSections(tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].BodyMarkdown, Is.EqualTo("First"));
        Assert.That(result[1].BodyMarkdown, Is.EqualTo("Second"));
    }

    [Test]
    public void ParseSections_BodyMarkdown_ExtractsEverythingAfterClosingDashes()
    {
        // Arrange
        var content = """
            ---
            title: Test
            ---
            This is the body.
            It has multiple lines.
            ---
            Even more dashes.
            """;
        File.WriteAllText(Path.Combine(tempCoursePath, "test.md"), content);

        // Act
        var result = parser.ParseSections(tempCoursePath).ToList();

        // Assert
        Assert.That(result[0].BodyMarkdown, Is.EqualTo(
            string.Format("This is the body.{0}It has multiple lines.{0}---{0}Even more dashes.", Environment.NewLine)));
    }

    [Test]
    public void ParseSections_WithQuizQuestionsOnLesson_PopulatesQuizQuestions()
    {
        // Arrange
        var content = @"---
title: ""Quiz Time""
quiz: quiz-time-q1
quiz_questions:
  - text: ""What is 2+2?""
    options: [""3"", ""4"", ""5""]
    correct_index: 1
  - text: ""What is the capital of France?""
    options: [""London"", ""Berlin"", ""Paris""]
    correct_index: 2
---
Good luck!";
        File.WriteAllText(Path.Combine(tempCoursePath, "quiz.md"), content);

        // Act
        var result = parser.ParseSections(tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var section = result[0];
        Assert.That(section.QuizId, Is.EqualTo("quiz-time-q1"));
        Assert.That(section.QuizQuestions.Count(), Is.EqualTo(2));

        var q1 = section.QuizQuestions.First();
        Assert.That(q1.Text, Is.EqualTo("What is 2+2?"));
        Assert.That(q1.Options.Count, Is.EqualTo(3));
        Assert.That(q1.Options[1], Is.EqualTo("4"));
        Assert.That(q1.CorrectIndex, Is.EqualTo(1));

        var q2 = section.QuizQuestions.Last();
        Assert.That(q2.Text, Is.EqualTo("What is the capital of France?"));
        Assert.That(q2.CorrectIndex, Is.EqualTo(2));
    }

    [Test]
    public void ParseSections_TypeFrontmatter_IsIgnored()
    {
        // Arrange
        var content = @"---
title: ""Empty Quiz""
type: lesson
quiz: empty-quiz-q1
---
Wait, where are the questions?";
        File.WriteAllText(Path.Combine(tempCoursePath, "empty-quiz.md"), content);

        // Act
        var result = parser.ParseSections(tempCoursePath).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        var section = result[0];
        Assert.That(section.QuizId, Is.EqualTo("empty-quiz-q1"));
        Assert.That(section.QuizQuestions.Count(), Is.EqualTo(0));
    }
}
