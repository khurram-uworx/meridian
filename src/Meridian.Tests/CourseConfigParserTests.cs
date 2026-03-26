using NUnit.Framework;
using Uworx.Meridian.Exceptions;
using Uworx.Meridian.Infrastructure.CourseSource;

namespace Meridian.Tests;

[TestFixture]
public class CourseConfigParserTests
{
    private string _tempCoursePath;
    private CourseConfigParser _parser;

    [SetUp]
    public void Setup()
    {
        _tempCoursePath = Path.Combine(Path.GetTempPath(), "meridian_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempCoursePath);
        _parser = new CourseConfigParser();
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
        File.WriteAllText(Path.Combine(_tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = _parser.Parse(_tempCoursePath);

        // Assert
        Assert.That(result.Id, Is.EqualTo("course-123"));
        Assert.That(result.Title, Is.EqualTo("Introduction to Meridian"));
        Assert.That(result.Version, Is.EqualTo("1.0.0"));
        Assert.That(result.Author, Is.EqualTo("John Doe"));
        Assert.That(result.JiraProject, Is.EqualTo("MER"));
        Assert.That(result.EpicLabel, Is.EqualTo("course-epic"));
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
        File.WriteAllText(Path.Combine(_tempCoursePath, "course.yaml"), yaml);

        // Act
        var result = _parser.Parse(_tempCoursePath);

        // Assert
        Assert.That(result.Id, Is.EqualTo("course-123"));
        Assert.That(result.Title, Is.EqualTo("Introduction to Meridian"));
        Assert.That(result.JiraProject, Is.EqualTo("MER"));
        Assert.That(result.Version, Is.Null);
        Assert.That(result.Author, Is.Null);
        Assert.That(result.EpicLabel, Is.Null);
    }

    [Test]
    public void Parse_MissingIdField_ThrowsInvalidCourseExceptionWithId()
    {
        // Arrange
        var yaml = @"
title: Introduction to Meridian
jira_project: MER
";
        File.WriteAllText(Path.Combine(_tempCoursePath, "course.yaml"), yaml);

        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => _parser.Parse(_tempCoursePath));
        Assert.That(ex.Message, Does.Contain("id").IgnoreCase);
    }

    [Test]
    public void Parse_MissingTitleField_ThrowsInvalidCourseExceptionWithTitle()
    {
        // Arrange
        var yaml = @"
id: course-123
jira_project: MER
";
        File.WriteAllText(Path.Combine(_tempCoursePath, "course.yaml"), yaml);

        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => _parser.Parse(_tempCoursePath));
        Assert.That(ex.Message, Does.Contain("title").IgnoreCase);
    }

    [Test]
    public void Parse_MissingJiraProjectField_ThrowsInvalidCourseExceptionWithJiraProject()
    {
        // Arrange
        var yaml = @"
id: course-123
title: Introduction to Meridian
";
        File.WriteAllText(Path.Combine(_tempCoursePath, "course.yaml"), yaml);

        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => _parser.Parse(_tempCoursePath));
        Assert.That(ex.Message, Does.Contain("jira_project").IgnoreCase);
    }

    [Test]
    public void Parse_MissingCourseYaml_ThrowsInvalidCourseExceptionWithCourseYaml()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidCourseException>(() => _parser.Parse(_tempCoursePath));
        Assert.That(ex.Message, Does.Contain("course.yaml").IgnoreCase);
    }
}
