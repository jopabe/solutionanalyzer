using Jox.SolutionAnalyzer;
using System.CommandLine;

var msBuildPath = new Option<DirectoryInfo>("msBuildPath",
    () => new DirectoryInfo(@"c:\Program Files\Microsoft Visual Studio\2022\Preview\Msbuild\Current\Bin"),
    "Location of the MSBuild installation");
var repositoryRoot = new Argument<DirectoryInfo>("repositoryRoot", "The root dir of the repository"); // { Arity = ArgumentArity.OneOrMore };

var rootCommand = new RootCommand("Analyze all .NET solutions in a repo for projects and dependencies")
{
    msBuildPath,
    repositoryRoot
};

rootCommand.SetHandler(async context =>
{
    Parser.RegisterMSBuildLocation(context.ParseResult.GetValueForOption(msBuildPath));
    var rootDir = context.ParseResult.GetValueForArgument(repositoryRoot);
    var repo = await Parser.CrawlRepository(rootDir, context.GetCancellationToken());
    foreach (var sol in repo.Solutions)
    {
        Printer.PrintSolution(sol);
    }
});

return rootCommand.Invoke(args);
