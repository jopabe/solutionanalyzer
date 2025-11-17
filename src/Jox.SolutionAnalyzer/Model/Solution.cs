using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Jox.SolutionAnalyzer.Model;

public class Solution
{
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
    public required string RepositoryId { get; init; }
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 1)]
    public required string SolutionFileRelativePath { get; init; }
    public string? ParseIssue { get; init; }

    public virtual IReadOnlyList<MSBuildProject> MSBuildProjects { get; init; } = [];
    public virtual IReadOnlyList<NonMsBuildProject> NonMSBuildProjects { get; init; } = [];

    public virtual Repository Repository { get; init; } = null!;
}
