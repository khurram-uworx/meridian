using LibGit2Sharp;
using Uworx.Meridian.CourseSource;

namespace Uworx.Meridian.Infrastructure.CourseSource;

public class CourseSourceResolver : ICourseSourceResolver
{
    Task<CourseSourceResult> resolveLocalFolderAsync(CourseSourceLocator locator)
    {
        if (!Directory.Exists(locator.Uri))
            throw new DirectoryNotFoundException($"Local folder not found: {locator.Uri}");

        return Task.FromResult(new CourseSourceResult(locator.Uri, null));
    }

    Task<CourseSourceResult> resolveGitAsync(CourseSourceLocator locator)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "meridian", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);

        Repository.Clone(locator.Uri, tempPath);

        string? sourceRevision;
        using (var repo = new Repository(tempPath))
        {
            sourceRevision = repo.Head.Tip.Sha;
        }

        var resolvedPath = tempPath;
        if (!string.IsNullOrEmpty(locator.SubPath))
        {
            if (Path.IsPathRooted(locator.SubPath) || locator.SubPath.Contains(".."))
                throw new ArgumentException("Invalid subpath provided.", nameof(locator));

            resolvedPath = Path.GetFullPath(Path.Combine(tempPath, locator.SubPath));

            // Safety check to ensure the resolved path is still within the temp directory
            if (!resolvedPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid subpath provided.", nameof(locator));

            if (!Directory.Exists(resolvedPath))
                throw new DirectoryNotFoundException($"Subfolder not found in Git repository: {locator.SubPath}");
        }

        return Task.FromResult(new CourseSourceResult(resolvedPath, sourceRevision));
    }

    public async Task<CourseSourceResult> ResolveAsync(CourseSourceLocator locator)
    {
        return locator.SourceType switch
        {
            CourseSourceType.LocalFolder => await resolveLocalFolderAsync(locator),
            CourseSourceType.Git => await resolveGitAsync(locator),
            _ => throw new NotSupportedException($"Source type {locator.SourceType} is not supported.")
        };
    }
}
