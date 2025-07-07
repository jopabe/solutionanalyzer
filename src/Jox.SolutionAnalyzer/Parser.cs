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
        var projects = new List<MSBuildProject>();

        var packagesPropsLocations = repositoryRoot.GetFiles("Directory.Packages.props", SearchOption.AllDirectories);
        switch (packagesPropsLocations.Length)
        {
            case 0:
                break;
            case 1:
                var packagesProps = new Project(packagesPropsLocations[0].FullName, null, null, projectCollection);
                foreach (var item in packagesProps.ItemsIgnoringCondition)
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

        repository = new()
        {
            RepositoryId = repositoryRoot.Name,
            Solutions = solutions,
            Projects = projects,
            ParseIssue = issues.Any() ? string.Join(";", issues) : null,
        };
        
        foreach (var sln in repositoryRoot.GetFiles("*.sln", SearchOption.AllDirectories))
        {
            solutions.Add(await ParseSolution(sln, cancellationToken));
        }
        projects.AddRange(projectCache.Values);

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
                        RepositoryId = repository!.RepositoryId,
                        RelativePath = NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName, projectInSolution.AbsolutePath),
                        ProjectName = projectInSolution.ProjectName,
                        ProjectType = projectInSolution.ProjectType.ToString()
                    });
                }
            }
            return new()
            {
                RepositoryId = repository!.RepositoryId,
                SolutionFileRelativePath = relativePath,
                MSBuildProjects = projects,
                NonMSBuildProjects = otherProjects,
            };
        }
        catch (Exception ex)
        {
            return new()
            {
                RepositoryId = repository!.RepositoryId,
                SolutionFileRelativePath = relativePath,
                ParseIssue = ex.Message,
            };
        }
    }

    private async Task<MSBuildProject> ParseProject(ProjectInSolution projectInSolution, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var projectFile = new FileInfo(projectInSolution.AbsolutePath);
        var projectFileRelativePath = NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName, projectFile.FullName);
        try
        {
            if (!projectFile.Exists)
            {
                return new MSBuildProject()
                {
                    RepositoryId = repository!.RepositoryId,
                    ProjectFileRelativePath = projectFileRelativePath,
                    ProjectName = projectInSolution.ProjectName + " (File Not Found)",
                };
            }
            var proj = await Task.Run(() => projectCollection.LoadProject(projectFile.FullName), cancellationToken).ConfigureAwait(false);
            Console.Error.WriteLine("status: processing {0}", projectFile.FullName);

            var packageReferences = proj.GetItemsIgnoringCondition("PackageReference")
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
                            RepositoryId = repository!.RepositoryId,
                            ProjectFileRelativePath = projectFileRelativePath,
                            PackageName = r.EvaluatedInclude,
                            PackageVersion = version,
                        };
                    }).ToList();

            string? parseIssue = null;
            var packagesConfig = new FileInfo(Path.Combine(proj.DirectoryPath, "packages.config"));
            if (packagesConfig.Exists)
            {
                if (packageReferences.Any())
                {
                    parseIssue = "Warning: both PackageReference and packages.config found. Using PackageReference.";
                }
                else
                {
                    using var stream = packagesConfig.OpenRead();
                    var reader = new NuGet.Packaging.PackagesConfigReader(stream);
                    packageReferences.AddRange(reader.GetPackages(true).Select(p => new PackageReference()
                    {
                        RepositoryId = repository!.RepositoryId,
                        ProjectFileRelativePath = projectFileRelativePath,
                        PackageName = p.PackageIdentity.Id,
                        PackageVersion = p.PackageIdentity.Version.ToString(),
                        FromPackagesConfig = true
                    }));
                }
            }
            return new MSBuildProject()
            {
                RepositoryId = repository!.RepositoryId,
                ProjectFileRelativePath = projectFileRelativePath,
                ProjectName = projectInSolution.ProjectName,
                TargetFrameworkVersion = proj.GetPropertyValue("TargetFrameworkVersion"),
                TargetFramework = proj.GetPropertyValue("TargetFramework"),
                TargetFrameworks = proj.GetPropertyValue("TargetFrameworks"),
                ProjectReferences = proj.GetItemsIgnoringCondition("ProjectReference")
                    .Select(r => new ProjectReference()
                    {
                        RepositoryId = repository!.RepositoryId,
                        ProjectFileRelativePath = projectFileRelativePath,
                        ReferencedProjectFileRelativePath = NetFrameworkBackports.GetRelativePath(repositoryRoot.FullName, Path.Combine(proj.DirectoryPath, r.EvaluatedInclude))
                    }).ToList(),
                PackageReferences = packageReferences,
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
                            RepositoryId = repository!.RepositoryId,
                            ProjectFileRelativePath = projectFileRelativePath,
                            AssemblyName = r.EvaluatedInclude,
                            HintPath = r.GetMetadataValue("HintPath"),
                            RepositoryRelativePath = relativePath,
                        };
                    }).ToList(),
                ParseIssue = parseIssue,
            };
        }
        catch (Exception ex)
        {
            return new MSBuildProject()
            {
                RepositoryId = repository!.RepositoryId,
                ProjectFileRelativePath = projectFileRelativePath,
                ProjectName = projectInSolution.ProjectName,
                ParseIssue = ex.Message,
            };
        }
    }
}
