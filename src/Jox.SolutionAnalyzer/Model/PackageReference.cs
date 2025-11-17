using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jox.SolutionAnalyzer.Model;

public class PackageReference
{
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
    public required string RepositoryId { get; init; }
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 1)]
    public required string ProjectFileRelativePath { get; init; }
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 2)]
    public required string PackageName { get; init; }
    public required string PackageVersion { get; init; }
    public bool FromPackagesConfig { get; init; } = false;
}
