namespace Jox.SolutionAnalyzer.Model;

public class PackageReference
{
    public required string PackageName { get; init; }
    public required string PackageVersion { get; init; }
    public bool FromPackagesConfig { get; init; } = false;
}
