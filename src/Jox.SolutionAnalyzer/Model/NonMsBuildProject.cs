namespace Jox.SolutionAnalyzer.Model;

public class NonMsBuildProject
{
    public required string RelativePath { get; init; }

    public required string ProjectName { get; init; }
    public required string ProjectType { get; init; }
}
