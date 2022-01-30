using Microsoft.Build.Construction;
using System.CommandLine;

var solutionFile = new Argument<FileInfo[]>("solutionFile", "The solution file") { Arity = ArgumentArity.OneOrMore };

var rootCommand = new RootCommand("Analyze a .NET solution for projects and dependencies")
{
    solutionFile
};

rootCommand.SetHandler(async (FileInfo[] slnFiles) =>
{
    foreach (FileInfo slnFile in slnFiles)
    {
        var sln = SolutionFile.Parse(slnFile.FullName);
        await Printer.PrintSolutionAsync(sln, slnFile.FullName);
    }
}, solutionFile);


return rootCommand.Invoke(args);
