using Microsoft.Build.Construction;

internal class Printer
{
    internal static async Task PrintSolutionAsync(SolutionFile sln, string slnPath)
    {
        foreach (var proj in sln.ProjectsInOrder)
        {
            Console.WriteLine($"{slnPath};{proj.ProjectType};{proj.ProjectName};{proj.AbsolutePath}");
        }
        await Task.Delay(100);
    }
}
