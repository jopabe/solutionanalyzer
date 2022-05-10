namespace Jox.SolutionAnalyzer;

public class MSBuildProject
{
    public FileInfo ProjectFile { get; init; }
    public string ProjectFileRelativePath(Repository repo)
        => Path.GetRelativePath(repo.RootPath.FullName, ProjectFile.FullName);

    public string ProjectName { get; init; }
    public string ProjectType => "KnownToBeMSBuildFormat";
    public string TargetFrameworkVersion { get; init; }
    public string TargetFramework { get; init; }
    public string TargetFrameworks { get; init; }

    public IReadOnlyList<ProjectReference> ProjectReferences { get; init; } = Array.Empty<ProjectReference>();
    public IReadOnlyList<PackageReference> PackageReferences { get; init; } = Array.Empty<PackageReference>();
    public IReadOnlyList<AssemblyReference> AssemblyReferences { get; init; } = Array.Empty<AssemblyReference>();
    public Exception ParseIssue { get; init; }
}
