namespace Jox.SolutionAnalyzer;

public class PackageReference
{
    public string PackageName { get; init; }
    public string PackageVersion { get; init; }
    public bool FromPackagesConfig { get; init; } = false;
}
