using LibGit2Sharp;
using NUnit.Framework;
using Uworx.Meridian.CourseSource;
using Uworx.Meridian.Infrastructure.CourseSource;

namespace Meridian.Tests;

[TestFixture]
public class CourseSourceResolverTests
{
    private CourseSourceResolver _resolver;

    [SetUp]
    public void SetUp()
    {
        _resolver = new CourseSourceResolver();
    }

    [Test]
    public async Task ResolveAsync_LocalFolder_ReturnsSamePath()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        try
        {
            var locator = new CourseSourceLocator(CourseSourceType.LocalFolder, tempPath);

            // Act
            var result = await _resolver.ResolveAsync(locator);

            // Assert
            Assert.That(result.FolderPath, Is.EqualTo(tempPath));
            Assert.That(result.SourceRevision, Is.Null);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Test]
    public void ResolveAsync_NonExistentLocalPath_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var locator = new CourseSourceLocator(CourseSourceType.LocalFolder, nonExistentPath);

        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await _resolver.ResolveAsync(locator));
    }

    [Test]
    public async Task ResolveAsync_GitRoot_ClonesAndReturnsPath()
    {
        // Arrange
        var repoPath = Path.Combine(Path.GetTempPath(), "source-repo-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(repoPath);
        Repository.Init(repoPath);

        using (var repo = new Repository(repoPath))
        {
            var filePath = Path.Combine(repoPath, "test.txt");
            File.WriteAllText(filePath, "test content");
            Commands.Stage(repo, "*");
            var signature = new Signature("Tester", "tester@example.com", DateTimeOffset.Now);
            var commit = repo.Commit("Initial commit", signature, signature);
            var expectedSha = commit.Sha;

            var locator = new CourseSourceLocator(CourseSourceType.GitRoot, repoPath);

            // Act
            var result = await _resolver.ResolveAsync(locator);

            // Assert
            try
            {
                Assert.That(Directory.Exists(result.FolderPath), Is.True);
                Assert.That(File.Exists(Path.Combine(result.FolderPath, "test.txt")), Is.True);
                Assert.That(result.SourceRevision, Is.EqualTo(expectedSha));
            }
            finally
            {
                // Cleanup temp clone
                DeleteDirectory(result.FolderPath);
            }
        }

        // Cleanup source repo
        DeleteDirectory(repoPath);
    }

    [Test]
    public async Task ResolveAsync_GitSubfolder_ReturnsSubfolderPath()
    {
        // Arrange
        var repoPath = Path.Combine(Path.GetTempPath(), "source-repo-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(repoPath);
        Repository.Init(repoPath);

        using (var repo = new Repository(repoPath))
        {
            var subfolder = "courses/c1";
            var subfolderPath = Path.Combine(repoPath, subfolder);
            Directory.CreateDirectory(subfolderPath);
            File.WriteAllText(Path.Combine(subfolderPath, "course.yaml"), "title: Test");

            Commands.Stage(repo, "*");
            var signature = new Signature("Tester", "tester@example.com", DateTimeOffset.Now);
            repo.Commit("Initial commit", signature, signature);

            var locator = new CourseSourceLocator(CourseSourceType.GitSubfolder, repoPath, subfolder);

            // Act
            var result = await _resolver.ResolveAsync(locator);

            // Assert
            try
            {
                Assert.That(Directory.Exists(result.FolderPath), Is.True);
                Assert.That(File.Exists(Path.Combine(result.FolderPath, "course.yaml")), Is.True);
                Assert.That(result.FolderPath.EndsWith(subfolder.Replace('/', Path.DirectorySeparatorChar)), Is.True);
            }
            finally
            {
                // Get the root of the clone to delete it all
                var cloneRoot = result.FolderPath.Substring(0, result.FolderPath.IndexOf(subfolder.Replace('/', Path.DirectorySeparatorChar)) - 1);
                DeleteDirectory(cloneRoot);
            }
        }

        DeleteDirectory(repoPath);
    }

    [Test]
    public async Task ResolveAsync_GitSubfolder_InvalidPath_ThrowsArgumentException()
    {
        // Arrange
        var repoPath = Path.Combine(Path.GetTempPath(), "source-repo-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(repoPath);
        Repository.Init(repoPath);

        using (var repo = new Repository(repoPath))
        {
            var filePath = Path.Combine(repoPath, "test.txt");
            File.WriteAllText(filePath, "test content");
            Commands.Stage(repo, "*");
            var signature = new Signature("Tester", "tester@example.com", DateTimeOffset.Now);
            repo.Commit("Initial commit", signature, signature);

            var locator = new CourseSourceLocator(CourseSourceType.GitSubfolder, repoPath, "../../etc/passwd");

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await _resolver.ResolveAsync(locator));
        }

        DeleteDirectory(repoPath);
    }

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        // LibGit2Sharp leaves read-only files in .git folder, need to handle them
        foreach (var directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
        {
            RemoveReadOnlyAttribute(directory);
        }

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            RemoveReadOnlyAttribute(file);
        }

        Directory.Delete(path, true);
    }

    private static void RemoveReadOnlyAttribute(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }
}
