using Jox.SolutionAnalyzer.Model;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Jox.SolutionAnalyzer;

public class Parser(DirectoryInfo repositoryRoot)
{
    private ProjectCollection projectCollection = new();
    private Dictionary<string, string> packageVersionLookup = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, MSBuildProject> projectCache = new(StringComparer.OrdinalIgnoreCase);
    private Repository? repository;
    private List<string> issues = new();

    public async Task<Repository> CrawlRepository(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (repository != null)
        {
            return repository;
        }
        var solutions = new List<Solution>();

        var packagesPropsLocations = repositoryRoot.GetFiles("Directory.Packages.props", SearchOption.AllDirectories);
        switch (packagesPropsLocations.Length)
        {
            case 0:
                break;
            case 1:
                var packagesProps = new Project(packagesPropsLocations[0].FullName, null, null, projectCollection);
                foreach (var item in packagesProps.Items)
                {
                    if (item.ItemType == "PackageVersion")
                    {
                        packageVersionLookup[item.EvaluatedInclude] = item.GetMetadataValue("Version");
                    }
                }
                break;
            default:
                issues.Add($"Repository {repositoryRoot.Name}: multiple Directory.Packages.props files found.");
                break;
        }

        foreach (var sln in repositoryRoot.GetFiles("*.sln", SearchOption.AllDirectories))
        {
            solutions.Add(await ParseSolution(sln, cancellationToken));
        }

        repository = new()
        {
            RootPath = repositoryRoot,
            Solutions = solutions,
            Issues = issues,
        };
        return repository;
    }

    public async Task<Solution> ParseSolution(FileInfo slnFile, CancellationToken cancellationToken)
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
                        project = await ParseProject(projectInSolution, cancellationToken).ConfigureAwait(false);
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

    private async Task<MSBuildProject> ParseProject(ProjectInSolution projectInSolution, CancellationToken cancellationToken)
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
            var proj = await Task.Run(() => projectCollection.LoadProject(projectFile.FullName), cancellationToken).ConfigureAwait(false);
            Console.Error.WriteLine("status: processing {0}", projectFile.FullName);
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
                    .Select(r =>
                    {
                        var version = r.GetMetadataValue("Version");
                        if (string.IsNullOrEmpty(version))
                        {
                            if (packageVersionLookup.TryGetValue(r.EvaluatedInclude, out var centralVersion))
                            {
                                version = centralVersion;
                            }
                        }
                        return new PackageReference()
                        {
                            PackageName = r.EvaluatedInclude,
                            PackageVersion = version,
                        };
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

    private static IEnumerable<PackageReference> ReadPackagesConfig(Project proj)
    {
        var packagesConfig = new FileInfo(Path.Combine(proj.DirectoryPath, "packages.config"));
        if (!packagesConfig.Exists)
        {
            return [];
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
