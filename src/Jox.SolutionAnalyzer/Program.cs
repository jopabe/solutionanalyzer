using Jox.SolutionAnalyzer;
using Jox.SolutionAnalyzer.Model;
using System.CommandLine;

var msBuildPath = new Option<DirectoryInfo>("--msBuildPath") { Description = "Location of the MSBuild installation" };
var repositoryRoot = new Argument<DirectoryInfo>("repositoryRoot") { Description = "The root dir of the repository" }; // { Arity = ArgumentArity.OneOrMore };
var insertIntoDatabase = new Option<bool?>("--insert") { Description = "Insert the result into the database", Arity = ArgumentArity.Zero };
var useDgml = new Option<bool?>("--dgml") { Description = "Use DGML format for the output", Arity = ArgumentArity.Zero };

var rootCommand = new RootCommand("Analyze all .NET solutions in a repo for projects and dependencies")
{
    msBuildPath,
    repositoryRoot,
    insertIntoDatabase,
    useDgml,
};

var truncateDatabase = new Command("truncate") { Description = "Truncate the database" };
rootCommand.Subcommands.Add(truncateDatabase);

truncateDatabase.SetAction(async(parseResult, cancellationToken) =>
{
    try
    {
        var dbcontext = new Sbom();
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM PackageReferences");
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM AssemblyReferences");
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM ProjectReferences");
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM MSBuildProjects");
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM NonMsBuildProjects");
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM Solutions");
        dbcontext.Database.ExecuteSqlCommand("DELETE FROM Repositories");
        await dbcontext.SaveChangesAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error truncating database: {ex}");
        throw;
    }
});

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    MSBuildIntegration.RegisterMSBuildLocation(parseResult.GetValue(msBuildPath));
    var rootDir = parseResult.GetRequiredValue(repositoryRoot);
    var repo = await new Parser(rootDir).CrawlRepository(cancellationToken);
    var insertDb = parseResult.GetValue(insertIntoDatabase) ?? false;
    if (insertDb)
    {
        try
        {
            var dbcontext = new Sbom();
            dbcontext.AddRepository(repo);
            await dbcontext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error inserting into database: {ex}");
            throw;
        }
    }
    else if (parseResult.GetValue(useDgml) ?? false)
    {
        var sbom = new Sbom();
        sbom.AddRepository(repo);
        var builder = new GraphBuilder(sbom);
        var graph = builder.GetDgml();

        // stdout
        graph.SerializeTo(Console.Out);
    }
    else
    {
        foreach (var sol in repo.Solutions)
        {
            Printer.PrintSolution(sol, repo);
        }
    }
});

ParseResult parseResult = rootCommand.Parse(args);
return parseResult.Invoke();
