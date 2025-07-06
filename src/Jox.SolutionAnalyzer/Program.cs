using Jox.SolutionAnalyzer;
using Jox.SolutionAnalyzer.Model;
using System.CommandLine;

var msBuildPath = new Option<DirectoryInfo>("--msBuildPath", "Location of the MSBuild installation");
var repositoryRoot = new Argument<DirectoryInfo>("repositoryRoot", "The root dir of the repository"); // { Arity = ArgumentArity.OneOrMore };
var insertIntoDatabase = new Option<bool?>("--insert", "Insert the result into the database") { Arity = ArgumentArity.Zero };

var rootCommand = new RootCommand("Analyze all .NET solutions in a repo for projects and dependencies")
{
    msBuildPath,
    repositoryRoot,
    insertIntoDatabase,
};

rootCommand.SetHandler(async context =>
{
    MSBuildIntegration.RegisterMSBuildLocation(context.ParseResult.GetValueForOption(msBuildPath));
    var rootDir = context.ParseResult.GetValueForArgument(repositoryRoot);
    var repo = await new Parser(rootDir).CrawlRepository(context.GetCancellationToken());
    var insertDb = context.ParseResult.GetValueForOption(insertIntoDatabase) ?? false;
    if (insertDb)
    {
        var dbcontext = new Sbom();
        dbcontext.AddRepository(repo);
        await dbcontext.SaveChangesAsync(context.GetCancellationToken());
    }
    else
    {
        foreach (var sol in repo.Solutions)
        {
            Printer.PrintSolution(sol, repo);
        }
    }
});

return rootCommand.Invoke(args);
