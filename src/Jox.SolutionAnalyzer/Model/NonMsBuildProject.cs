using System.ComponentModel.DataAnnotations;

namespace Jox.SolutionAnalyzer.Model;

public class NonMsBuildProject
{
    [Key()]
    public int NonMsBuildProjectId { get; set; }
    public required string RepositoryId { get; init; }
    public required string RelativePath { get; init; }

    public required string ProjectName { get; init; }
    public required string ProjectType { get; init; }
}
