namespace Jox.SolutionAnalyzer.Model;

public class Repository
{
    public required DirectoryInfo RootPath { get; init;  }
    public required IReadOnlyList<Solution> Solutions { get; init; }
}
