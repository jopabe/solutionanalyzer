using Microsoft.Build.Construction;
namespace Jox.SolutionAnalyzer.Model;

public class Solution
{
    public required string SolutionFileRelativePath { get; init; }
    public IReadOnlyList<MSBuildProject> MSBuildProjects { get; init; } = [];
    public IReadOnlyList<NonMsBuildProject> NonMSBuildProjects { get; init; } = [];

    public Exception? ParseIssue { get; init; }
}
