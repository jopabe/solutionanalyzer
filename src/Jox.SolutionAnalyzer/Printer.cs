using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
namespace Jox.SolutionAnalyzer;

internal class Printer
{
    internal static void PrintSolution(Solution sln)
    {
        foreach (var proj in sln.NonMSBuildProjects)
        {
            WriteCsvLine("sln-proj", sln.SolutionFilePath.FullName, proj.AbsolutePath,
                proj.ProjectType, proj.ProjectName, string.Empty, string.Empty, string.Empty);
        }

        foreach (var proj in sln.MSBuildProjects)
        {
            PrintProject(proj, sln.SolutionFilePath.FullName);
        }
    }

    internal static void PrintProject(MSBuildProject proj, string slnPath)
    {
        WriteCsvLine("sln-proj", slnPath, proj.ProjectFile.FullName,
            proj.ProjectType, proj.ProjectName, proj.TargetFrameworkVersion,
            proj.TargetFramework, proj.TargetFrameworks);
        foreach (var projectRef in proj.ProjectReferences)
        {
            WriteCsvLine("proj-projdep", proj.ProjectFile.FullName, projectRef.ProjectFile.FullName);
        }
        foreach (var assemblyRef in proj.AssemblyReferences)
        {
            WriteCsvLine("proj-dep", proj.ProjectFile.FullName, assemblyRef.AssemblyName, assemblyRef.HintPath);
        }
        foreach (var packageRef in proj.PackageReferences)
        {
            WriteCsvLine("proj-packagedep", proj.ProjectFile.FullName, packageRef.PackageName, packageRef.PackageVersion);
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
