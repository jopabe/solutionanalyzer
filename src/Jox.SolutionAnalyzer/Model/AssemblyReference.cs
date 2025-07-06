using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jox.SolutionAnalyzer.Model;

public class AssemblyReference
{
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
    public required string RepositoryId { get; init; }
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 1)]
    public required string ProjectFileRelativePath { get; init; }
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 2)]
    public required string AssemblyName { get; init; }
    public required string HintPath { get; init; }
    public required string RepositoryRelativePath { get; init; }
}
