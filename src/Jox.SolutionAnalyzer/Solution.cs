using Microsoft.Build.Construction;
namespace Jox.SolutionAnalyzer;

public class Solution
{
    public FileInfo SolutionFilePath { get; init; }
    public string SolutionFileRelativePath(Repository repo)
        => Path.GetRelativePath(repo.RootPath.FullName, SolutionFilePath.FullName);
    public IReadOnlyList<MSBuildProject> MSBuildProjects { get; init; }
    public IReadOnlyList<NonMsBuildProject> NonMSBuildProjects { get; init; }

}

