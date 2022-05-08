using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using NuGet.Packaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Jox.SolutionAnalyzer;

public class Parser
{
    public static void RegisterMSBuildLocation(DirectoryInfo? path)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            if (path == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // when running in net6.0, MSBuildLocator.QueryVisualStudioInstances() can't find
                // MSBuild versions that support building pre-SDK projects, so use vswhere 
                var latestVisualStudio = DetectLatestVisualStudio();
                if (latestVisualStudio != null)
                {
                    path = new DirectoryInfo(Path.Combine(latestVisualStudio, @"Msbuild\Current\Bin"));
                }
            }
            if (path != null && path.Exists)
            {
                MSBuildLocator.RegisterMSBuildPath(path.FullName);
            }
            else
            {
                var latest = MSBuildLocator.QueryVisualStudioInstances().MaxBy(i => i.Version);
                MSBuildLocator.RegisterInstance(latest);
            }
        }
    }

    private static string? DetectLatestVisualStudio()
    {
        try
        {
            var vswhereInfo = new ProcessStartInfo()
            {
                FileName = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe",
                Arguments = "-requires Microsoft.Component.MSBuild -property installationPath -latest -prerelease -version '[16.0,'",
                UseShellExecute = true,
                RedirectStandardOutput = true
            };
            if (!File.Exists(vswhereInfo.FileName)) return null;

            using var vswhere = Process.Start(vswhereInfo);
            if (vswhere == null) return null;
            vswhere.WaitForExit();
            if (vswhere.ExitCode != 0) return null;
            return vswhere!.StandardOutput.ReadToEnd();
        }
        catch (Exception)
        {
            // no worries
            return null;
        }
    }

    public static async Task<Repository> CrawlRepository(DirectoryInfo rootDir, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var solutions = new List<Solution>();
        foreach (var sln in rootDir.GetFiles("*.sln", SearchOption.AllDirectories))
        {
            solutions.Add(await ParseSolution(sln, cancellationToken));
        }
        return new Repository(rootDir) { Solutions = solutions };
    }

    public static async Task<Solution> ParseSolution(FileInfo slnFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var msBuildSolution = await Task.Run(() => SolutionFile.Parse(slnFile.FullName), cancellationToken).ConfigureAwait(false);
        var projects = new List<MSBuildProject>();
        var otherProjects = new List<NonMsBuildProject>();

        foreach (var projectInSolution in msBuildSolution.ProjectsInOrder)
        {
            if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            {
                projects.Add(await ParseProject(projectInSolution, cancellationToken).ConfigureAwait(false));
            }
            else
            {
                otherProjects.Add(new NonMsBuildProject()
                {
                    AbsolutePath = projectInSolution.AbsolutePath,
                    ProjectName = projectInSolution.ProjectName,
                    ProjectType = projectInSolution.ProjectType.ToString()
                });
            }
        }
        return new Solution() { SolutionFilePath = slnFile, MSBuildProjects = projects, NonMSBuildProjects = otherProjects };
    }

    private static async Task<MSBuildProject> ParseProject(ProjectInSolution projectInSolution, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var proj = await Task.Run(() => Project.FromFile(projectInSolution.AbsolutePath, new ProjectOptions()), cancellationToken).ConfigureAwait(false);
        return new MSBuildProject()
        {
            ProjectFile = new FileInfo(proj.FullPath),
            ProjectName = projectInSolution.ProjectName,
            TargetFrameworkVersion = proj.GetPropertyValue("TargetFrameworkVersion"),
            TargetFramework = proj.GetPropertyValue("TargetFramework"),
            TargetFrameworks = proj.GetPropertyValue("TargetFrameworks"),
            ProjectReferences = proj.GetItemsIgnoringCondition("ProjectReference")
                .Select(r => new ProjectReference()
                {
                    ProjectFile = new FileInfo(Path.Combine(proj.DirectoryPath, r.EvaluatedInclude))
                }).ToList(),
            PackageReferences = proj.GetItemsIgnoringCondition("PackageReference")
                .Select(r => new PackageReference()
                {
                    PackageName = r.EvaluatedInclude,
                    PackageVersion = r.GetMetadataValue("Version")
                }).Concat(ReadPackagesConfig(proj)).ToList(),
            AssemblyReferences = proj.GetItemsIgnoringCondition("Reference")
                .Select(r => new AssemblyReference()
                {
                    AssemblyName = r.EvaluatedInclude,
                    HintPath = r.GetMetadataValue("HintPath")
                }).ToList()
        };
    }

    private static IEnumerable<PackageReference> ReadPackagesConfig(Project proj)
    {
        var packagesConfig = new FileInfo(Path.Combine(proj.DirectoryPath, "packages.config"));
        if (!packagesConfig.Exists)
        {
            return Enumerable.Empty<PackageReference>();
        }
        using var stream = packagesConfig.OpenRead();
        var reader = new PackagesConfigReader(stream);
        return reader.GetPackages(true).Select(p => new PackageReference()
        {
            PackageName = p.PackageIdentity.Id,
            PackageVersion = p.PackageIdentity.Version.ToString(),
            FromPackagesConfig = true
        });
    }
}
