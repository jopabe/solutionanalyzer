using Microsoft.Build.Construction;
namespace Jox.SolutionAnalyzer;

public class Solution
{
    public SolutionFile MSBuildSolution { get; init; }
    public FileInfo SolutionFilePath { get; init; }
    public IReadOnlyList<MSBuildProject> MSBuildProjects { get; init; }
    public IReadOnlyList<NonMsBuildProject> NonMSBuildProjects { get; init; }

}

