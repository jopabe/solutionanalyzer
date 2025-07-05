namespace Jox.SolutionAnalyzer.Model;

public class MSBuildProject
{
    public required string ProjectFileRelativePath { get; init; }
    public required string ProjectName { get; init; }
    public string ProjectType => "KnownToBeMSBuildFormat";
    public string? TargetFrameworkVersion { get; init; }
    public string? TargetFramework { get; init; }
    public string? TargetFrameworks { get; init; }

    public IReadOnlyList<ProjectReference> ProjectReferences { get; init; } = [];
    public IReadOnlyList<PackageReference> PackageReferences { get; init; } = [];
    public IReadOnlyList<AssemblyReference> AssemblyReferences { get; init; } = [];
    public Exception? ParseIssue { get; init; }
}
