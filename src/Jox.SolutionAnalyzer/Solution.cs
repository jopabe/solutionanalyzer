using Microsoft.Build.Construction;
namespace Jox.SolutionAnalyzer;

public class Solution
{
    public FileInfo SolutionFilePath { get; init; }
    public string SolutionFileRelativePath(Repository repo)
        => Path.GetRelativePath(repo.RootPath.FullName, SolutionFilePath.FullName);
    public IReadOnlyList<MSBuildProject> MSBuildProjects { get; init; } = Array.Empty<MSBuildProject>();
    public IReadOnlyList<NonMsBuildProject> NonMSBuildProjects { get; init; } = Array.Empty<NonMsBuildProject>();

    public Exception ParseIssue { get; init; }
}
