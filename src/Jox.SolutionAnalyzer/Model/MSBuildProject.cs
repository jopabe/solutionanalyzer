using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jox.SolutionAnalyzer.Model;

public class MSBuildProject
{
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
    public required string RepositoryId { get; init; }
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 1)]
    public required string ProjectFileRelativePath { get; init; }
    public required string ProjectName { get; init; }
    public string ProjectType => "KnownToBeMSBuildFormat";
    public string? TargetFrameworkVersion { get; init; }
    public string? TargetFramework { get; init; }
    public string? TargetFrameworks { get; init; }
    public string? PackageId { get; init; }
    public string? ParseIssue { get; init; }

    public IReadOnlyList<ProjectReference> ProjectReferences { get; init; } = [];
    public IReadOnlyList<PackageReference> PackageReferences { get; init; } = [];
    public IReadOnlyList<AssemblyReference> AssemblyReferences { get; init; } = [];

    public virtual Repository Repository { get; init; } = null!;
}
