namespace Jox.SolutionAnalyzer;

public class NonMsBuildProject
{
    public string AbsolutePath { get; init; }
    public string RelativePath(Repository repo)
        => Path.GetRelativePath(repo.RootPath.FullName, AbsolutePath);

    public string ProjectName { get; init; }
    public string ProjectType { get; init; }
}
