namespace Jox.SolutionAnalyzer;

public class Repository
{
    public DirectoryInfo RootPath { get; }

    public Repository(DirectoryInfo rootPath)
    {
        RootPath = rootPath;
    }

    public IReadOnlyList<Solution> Solutions { get; init; }
}
