using ConsoleAppFramework;

namespace Jox.SolutionAnalyzer;

internal class CliCommands
{
    /// <summary>
    /// Analyze all .NET solutions in a repo for projects and dependencies
    /// </summary>
    /// <param name="repositoryRoot">The root dir of the repository</param>
    /// <param name="msbuildPath">Location of the MSBuild installation</param>
    [Command("analyze")]
    public async Task Analyze([Argument] string repositoryRoot, CancellationToken cancellationToken, string? msbuildPath = null)
    {
        MSBuildIntegration.RegisterMSBuildLocation(msbuildPath is null ? null : new(msbuildPath));
        var repo = await Parser.CrawlRepository(new(repositoryRoot), cancellationToken);
        foreach (var sol in repo.Solutions)
        {
            Printer.PrintSolution(sol, repo);
        }
    }
}
