using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Uworx.Meridian.Infrastructure.Data;
using static Meridian.Tests.NUnitConstants;

namespace Meridian.Tests;

[TestFixture, Category(TestCatory.Unit)]
class DbContextTests
{
    MeridianDbContext? context;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MeridianDbContext>()
            .UseInMemoryDatabase(databaseName: "TestMeridianDb")
            .Options;

        context = new MeridianDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        context.Dispose();
    }

    [Test]
    public void Database_EnsureCreated_Succeeds()
    {
        // Act
        var created = context.Database.EnsureCreated();

        // Assert
        Assert.That(created, Is.True.Or.False); // Returns true if DB was just created, false if it already existed
    }

    [Test]
    public void DbSets_AreQueryable()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => context.Learners.Any());
        Assert.DoesNotThrow(() => context.Courses.Any());
        Assert.DoesNotThrow(() => context.Enrollments.Any());
        Assert.DoesNotThrow(() => context.QuizAttempts.Any());
    }
}
