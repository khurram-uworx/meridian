using NUnit.Framework;
using Uworx.Meridian.Exceptions;
using Uworx.Meridian.Infrastructure.CourseSource;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class CourseConfigParserTests
{
    string? tempCoursePath;
    CourseConfigParser? parser;

    [SetUp]
    public void Setup()
    {
        tempCoursePath = Path.Combine(Path.GetTempPath(), "meridian_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempCoursePath);
        parser = new CourseConfigParser();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempCoursePath))
        {
            Directory.Delete(tempCoursePath, true);
        }
    }

    [Test]
    public void Parse_ValidYaml_ReturnsCorrectCourseConfig()
    {
        // Arrange
        var yaml = @"
id: course-123
title: Introduction to Meridian
version: 1.0.0
author: John Doe
jira_project: MER
epic_label: course-epic
";
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = parser.Parse(tempCoursePath);

        // Assert
        Assert.That(result.Id, Is.EqualTo("course-123"));
        Assert.That(result.Title, Is.EqualTo("Introduction to Meridian"));
        Assert.That(result.Version, Is.EqualTo("1.0.0"));
        Assert.That(result.Author, Is.EqualTo("John Doe"));
        Assert.That(result.JiraProject, Is.EqualTo("MER"));
        Assert.That(result.EpicLabel, Is.EqualTo("course-epic"));
        Assert.That(result.EpicDescription, Is.Null);
    }

    [Test]
    public void Parse_ValidYamlMissingOptionalFields_ReturnsCorrectCourseConfig()
    {
        // Arrange
        var yaml = @"
id: course-123
title: Introduction to Meridian
jira_project: MER
";
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = parser.Parse(tempCoursePath);

        // Assert
        Assert.That(result.Id, Is.EqualTo("course-123"));
        Assert.That(result.Title, Is.EqualTo("Introduction to Meridian"));
        Assert.That(result.JiraProject, Is.EqualTo("MER"));
        Assert.That(result.Version, Is.Null);
        Assert.That(result.Author, Is.Null);
        Assert.That(result.EpicLabel, Is.Null);
        Assert.That(result.EpicDescription, Is.Null);
    }

    [Test]
    public void Parse_ValidYamlWithEpicDescriptionField_ReturnsEpicDescription()
    {
        // Arrange
        var yaml = @"
id: course-123
title: Introduction to Meridian
jira_project: MER
epic_description: |
  Intro line 1
  Intro line 2
";
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = parser.Parse(tempCoursePath);

        // Assert
        Assert.That(result.EpicDescription, Is.EqualTo("Intro line 1\nIntro line 2"));
    }

    [Test]
    public void Parse_YamlWithTrailingIntroText_ReturnsEpicDescriptionFromBody()
    {
        // Arrange
        var yaml = """
            ---
            id: course-123
            title: Introduction to Meridian
            jira_project: MER
            ---
            This is the intro text.

            It should become Epic description.
            """;
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = parser.Parse(tempCoursePath);

        // Assert
        Assert.That(result.EpicDescription, Is.EqualTo("This is the intro text.\n\nIt should become Epic description."));
    }

    [Test]
    public void Parse_EpicDescriptionFieldTakesPrecedenceOverTrailingIntroText()
    {
        // Arrange
        var yaml = """
            ---
            id: course-123
            title: Introduction to Meridian
            jira_project: MER
            epic_description: Preferred description
            ---
            Fallback description
            """;
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = parser.Parse(tempCoursePath);

        // Assert
        Assert.That(result.EpicDescription, Is.EqualTo("Preferred description"));
    }

    [Test]
    public void Parse_MissingIdField_ThrowsInvalidCourseExceptionWithId()
    {
        // Arrange
        var yaml = """
            title: Introduction to Meridian
            jira_project: MER
            """;
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => parser.Parse(tempCoursePath));
        Assert.That(ex.Message, Does.Contain("id").IgnoreCase);
    }

    [Test]
    public void Parse_MissingTitleField_ThrowsInvalidCourseExceptionWithTitle()
    {
        // Arrange
        var yaml = """
            id: course-123
            jira_project: MER
            """;
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => parser.Parse(tempCoursePath));
        Assert.That(ex.Message, Does.Contain("title").IgnoreCase);
    }

    [Test]
    public void Parse_MissingJiraProjectField_ThrowsInvalidCourseExceptionWithJiraProject()
    {
        // Arrange
        var yaml = """
            id: course-123
            title2: Introduction to Meridian
            """;
        File.WriteAllText(Path.Combine(tempCoursePath, "course.yaml"), yaml);

        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => parser.Parse(tempCoursePath));
        Assert.That(ex.Message, Does.Contain("title").IgnoreCase);
    }

    [Test]
    public void Parse_MissingCourseYaml_ThrowsInvalidCourseExceptionWithCourseYaml()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => parser.Parse(tempCoursePath));
        Assert.That(ex.Message, Does.Contain("course.yaml").IgnoreCase);
    }
}
