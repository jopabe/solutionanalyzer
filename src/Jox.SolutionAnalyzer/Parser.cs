using Jox.SolutionAnalyzer.Model;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;

namespace Jox.SolutionAnalyzer;

public class Parser
{
    public static async Task<Repository> CrawlRepository(DirectoryInfo rootDir, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var solutions = new List<Solution>();
        foreach (var sln in rootDir.GetFiles("*.sln", SearchOption.AllDirectories))
        {
            solutions.Add(await ParseSolution(sln, rootDir, cancellationToken));
        }
        return new()
        {
            RootPath = rootDir,
            Solutions = solutions,
        };
    }

    public static async Task<Solution> ParseSolution(FileInfo slnFile, DirectoryInfo repositoryRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var relativePath = NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName, slnFile.FullName);
        try
        {
            var msBuildSolution = await Task.Run(() => SolutionFile.Parse(slnFile.FullName), cancellationToken).ConfigureAwait(false);
            var projects = new List<MSBuildProject>();
            var otherProjects = new List<NonMsBuildProject>();

            foreach (var projectInSolution in msBuildSolution.ProjectsInOrder)
            {
                if (projectInSolution.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                {
                    MSBuildProject? project = null;
                    if (!projectCache.TryGetValue(projectInSolution.AbsolutePath, out project))
                    {
                        project = await ParseProject(projectInSolution, repositoryRoot, cancellationToken).ConfigureAwait(false);
                        projectCache.Add(projectInSolution.AbsolutePath, project);
                    }
                    projects.Add(project);
                }
                else
                {
                    otherProjects.Add(new NonMsBuildProject()
                    {
                        RelativePath = NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName, projectInSolution.AbsolutePath),
                        ProjectName = projectInSolution.ProjectName,
                        ProjectType = projectInSolution.ProjectType.ToString()
                    });
                }
            }
            return new() { SolutionFileRelativePath = relativePath, MSBuildProjects = projects, NonMSBuildProjects = otherProjects };
        }
        catch (Exception ex)
        {
            return new() { SolutionFileRelativePath = relativePath, ParseIssue = ex };
        }
    }

    private static async Task<MSBuildProject> ParseProject(ProjectInSolution projectInSolution, DirectoryInfo repositoryRoot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var projectFile = new FileInfo(projectInSolution.AbsolutePath);
        var relativePath = NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName, projectFile.FullName);
        try
        {
            if (!projectFile.Exists)
            {
                return new MSBuildProject()
                {
                    ProjectFileRelativePath = relativePath,
                    ProjectName = projectInSolution.ProjectName + " (File Not Found)"
                };
            }
            var proj = ProjectCollection.GlobalProjectCollection.LoadedProjects.FirstOrDefault(p => projectFile.FullName.Equals(p.FullPath, StringComparison.OrdinalIgnoreCase));
            if (proj == null)
            {
                Console.Error.WriteLine("status: processing {0}", projectFile.FullName);
                proj = await Task.Run(() => Project.FromFile(projectFile.FullName, new ProjectOptions()), cancellationToken).ConfigureAwait(false);
            }
            return new MSBuildProject()
            {
                ProjectFileRelativePath = relativePath,
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
                    .Where(r => !r.HasMetadata("IsImplicitlyDefined"))
                    .Select(r => new PackageReference()
                    {
                        PackageName = r.EvaluatedInclude,
                        PackageVersion = r.GetMetadataValue("Version")
                    }).Concat(ReadPackagesConfig(proj)).ToList(),
                AssemblyReferences = proj.GetItemsIgnoringCondition("Reference")
                    .Where(r => !r.HasMetadata("IsImplicitlyDefined"))
                    .Select(r =>
                    {
                        var hintPath = r.GetMetadataValue("HintPath");
                        var relativePath = string.IsNullOrWhiteSpace(hintPath) ? "" :
                            NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName,
                                Path.Combine(projectFile.Directory!.FullName, hintPath));
                        return new AssemblyReference()
                        {
                            AssemblyName = r.EvaluatedInclude,
                            HintPath = r.GetMetadataValue("HintPath"),
                            RepositoryRelativePath = relativePath,
                        };
                    }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new MSBuildProject()
            {
                ProjectFileRelativePath = relativePath,
                ProjectName = projectInSolution.ProjectName,
                ParseIssue = ex
            };
        }
    }

    private static Dictionary<string, MSBuildProject> projectCache = new();
    private static IEnumerable<PackageReference> ReadPackagesConfig(Project proj)
    {
        var packagesConfig = new FileInfo(Path.Combine(proj.DirectoryPath, "packages.config"));
        if (!packagesConfig.Exists)
        {
            return Enumerable.Empty<PackageReference>();
        }
        using var stream = packagesConfig.OpenRead();
        var reader = new NuGet.Packaging.PackagesConfigReader(stream);
        return reader.GetPackages(true).Select(p => new PackageReference()
        {
            PackageName = p.PackageIdentity.Id,
            PackageVersion = p.PackageIdentity.Version.ToString(),
            FromPackagesConfig = true
        });
    }
}
