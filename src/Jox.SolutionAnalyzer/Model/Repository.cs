using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jox.SolutionAnalyzer.Model;

public class Repository
{   
    [Key(), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required string RepositoryId { get; init;  }
    public virtual required IReadOnlyList<Solution> Solutions { get; init; }
    public string? ParseIssue { get; init; }
}
