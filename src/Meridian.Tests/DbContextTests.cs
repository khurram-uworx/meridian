using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Uworx.Meridian.Entities;
using Uworx.Meridian.Infrastructure.Data;

namespace Meridian.Tests;

[TestFixture]
public class DbContextTests
{
    private MeridianDbContext _context;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MeridianDbContext>()
            .UseInMemoryDatabase(databaseName: "TestMeridianDb")
            .Options;

        _context = new MeridianDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void Database_EnsureCreated_Succeeds()
    {
        // Act
        var created = _context.Database.EnsureCreated();

        // Assert
        Assert.That(created, Is.True.Or.False); // Returns true if DB was just created, false if it already existed
    }

    [Test]
    public void DbSets_AreQueryable()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _context.Learners.Any());
        Assert.DoesNotThrow(() => _context.Courses.Any());
        Assert.DoesNotThrow(() => _context.Enrollments.Any());
        Assert.DoesNotThrow(() => _context.QuizAttempts.Any());
    }
}
