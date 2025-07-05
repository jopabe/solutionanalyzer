namespace Jox.SolutionAnalyzer.Model;

public class AssemblyReference
{
    public required string AssemblyName { get; init; }
    public required string HintPath { get; init; }
    public required string RepositoryRelativePath { get; init; }
}
