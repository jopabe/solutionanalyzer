using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
namespace Jox.SolutionAnalyzer;

internal class Printer
{
    internal static void PrintSolution(Solution sln, Repository repo)
    {
        if (sln.ParseIssue != null)
        {
            WriteCsvLine("sln-corrupt", sln.SolutionFileRelativePath(repo), sln.ParseIssue.Message);
        }
        foreach (var proj in sln.NonMSBuildProjects)
        {
            WriteCsvLine("sln-proj", sln.SolutionFileRelativePath(repo), proj.RelativePath(repo),
                proj.ProjectType, proj.ProjectName, string.Empty, string.Empty, string.Empty);
        }

        foreach (var proj in sln.MSBuildProjects)
        {
            PrintProject(proj, sln.SolutionFileRelativePath(repo), repo);
        }
    }

    internal static void PrintProject(MSBuildProject proj, string slnPath, Repository repo)
    {
        var projectFileRelativePath = proj.ProjectFileRelativePath(repo);
        if (proj.ParseIssue != null)
        {
            WriteCsvLine("sln-proj-corrupt", slnPath, projectFileRelativePath,
                proj.ProjectType, proj.ProjectName, proj.ParseIssue.Message, string.Empty, string.Empty);
        }
        else
        {
            WriteCsvLine("sln-proj", slnPath, projectFileRelativePath,
                proj.ProjectType, proj.ProjectName, proj.TargetFrameworkVersion,
                proj.TargetFramework, proj.TargetFrameworks);
        }
        foreach (var projectRef in proj.ProjectReferences)
        {
            WriteCsvLine("proj-projdep", projectFileRelativePath, projectRef.ProjectFile.FullName);
        }
        foreach (var assemblyRef in proj.AssemblyReferences)
        {
            WriteCsvLine("proj-dep", projectFileRelativePath,
                assemblyRef.AssemblyName,
                string.IsNullOrWhiteSpace(assemblyRef.HintPath) ? "" : Path.GetRelativePath(
                    repo.RootPath.FullName,
                    Path.Combine(proj.ProjectFile.Directory!.FullName, assemblyRef.HintPath)));
        }
        foreach (var packageRef in proj.PackageReferences)
        {
            WriteCsvLine("proj-packagedep", projectFileRelativePath,
                packageRef.PackageName, packageRef.PackageVersion,
                packageRef.FromPackagesConfig ? "packages.config" : "PackageReference");
        }
    }

    private static void WriteCsvLine(params object[] values)
    {
        Console.Write('"');
        bool first = true;
        foreach (object value in values)
        {
            if (first) { first = false; } else { Console.Write("\";\""); }
            var stringValue = value?.ToString();
            if (stringValue != null) Console.Write(stringValue.Replace("\"", "\"\""));
        }
        Console.WriteLine('"');
    }
}
