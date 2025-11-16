using Jox.SolutionAnalyzer.Model;
using OpenSoftware.DgmlTools;
using OpenSoftware.DgmlTools.Builders;
using OpenSoftware.DgmlTools.Model;

namespace Jox.SolutionAnalyzer;

internal class GraphBuilder(Sbom sbom)
{
    public DirectedGraph GetDgml()
    {
        PackageNameForProject.Clear();
        foreach(var project in sbom.MSBuildProjects)
        {
            PackageNameForProject[project.ProjectFileRelativePath] = project.PackageId!;
        }
        var builder = new DgmlBuilder
        {
            NodeBuilders = [new NodesBuilder<GraphBuilder>(b => GetNodes())],
            LinkBuilders = [new LinksBuilder<GraphBuilder>(b => GetLinks())]
        };
        return builder.Build([this]);
    }

    private IEnumerable<Node> GetNodes()
    {
        foreach (var repo in sbom.Repositories)
        {
            yield return new()
            {
                Id = repo.RepositoryId,
                Label = repo.RepositoryId,
                Group = "Expanded",
            };

            foreach (var project in repo.Projects)
            {
                yield return new()
                {
                    Id = UniqueIdPreferringPackageName(project.ProjectFileRelativePath),
                    Label = project.ProjectName,
                };
                foreach (var package in project.PackageReferences)
                {
                    yield return new()
                    {
                        Id = package.PackageVersionUniqueId,
                        Label = package.ToString(),
                    };
                    var packageBundleName = PackageBundleName(package);
                    yield return new()
                    {
                        Id = packageBundleName.ToLowerInvariant(),
                        Label = packageBundleName,
                        Group = "Collapsed",
                    };
                }
            }
        }
    }

    private IEnumerable<Link> GetLinks()
    {
        foreach (var repo in sbom.Repositories)
        {

            foreach (var project in repo.Projects)
            {
                var projectId = UniqueIdPreferringPackageName(project.ProjectFileRelativePath);
                yield return new()
                {
                    Source = repo.RepositoryId,
                    Target = projectId,
                    Category = "Contains",
                };

                foreach (var package in project.PackageReferences)
                {
                    yield return new()
                    {
                        Source = projectId,
                        Target = package.PackageVersionUniqueId,
                    };
                    yield return new()
                    {
                        Source = PackageBundleName(package).ToLowerInvariant(),
                        Target = package.PackageVersionUniqueId,
                        Category = "Contains",
                    };

                }
                foreach (var reference in project.ProjectReferences)
                {
                    yield return new()
                    {
                        Source = UniqueIdPreferringPackageName(project.ProjectFileRelativePath),
                        Target = UniqueIdPreferringPackageName(reference.ReferencedProjectFileRelativePath),
                    };
                }
            }
        }
    }


    private Dictionary<string, string> PackageNameForProject { get; } = new(StringComparer.OrdinalIgnoreCase);
    private string UniqueIdPreferringPackageName(string projectUniqueId)
    {
        if (PackageNameForProject.TryGetValue(projectUniqueId, out var packageName))
        {
            return packageName;
        }
        // Fallback to the project unique ID if no package name is found
        return projectUniqueId;
    }
    private Dictionary<string, string> PackageBundleForPackageName { get; } = new(StringComparer.OrdinalIgnoreCase);

    private string PackageBundleName(PackageReference package)
    {
            if (!PackageBundleForPackageName.TryGetValue(package.PackageName, out var packageBundleName))
            {
                packageBundleName = package.PackageName;
                foreach (var namespacePrefix in packageBundles.Keys)
                {
                    if (package.PackageName.Equals(namespacePrefix, StringComparison.OrdinalIgnoreCase) ||
                        package.PackageName.StartsWith(namespacePrefix + ".", StringComparison.OrdinalIgnoreCase))
                    {
                        packageBundleName = packageBundles[namespacePrefix];
                        break;
                    }
                }
            PackageBundleForPackageName[package.PackageName] = packageBundleName;
            }
            return packageBundleName;
        }

    private readonly Dictionary<string, string> packageBundles = new()
    {
        ["Aspose"] = "Aspose",
        ["Azure"] = "Azure",
        ["CacheCow"] = "CacheCow",
        ["FluentValidation"] = "FluentValidation",
        ["Hangfire"] = "Hangfire",
        ["Janus"] = "Janus",
        ["Kadro"] = "Kadro",
        ["Microsoft"] = "Microsoft",
        ["MSTest"] = "MSTest",
        ["Newtonsoft.Json"] = "Newtonsoft.Json",
        ["NLog"] = "NLog",
        ["NSubstitute"] = "NSubstitute",
        ["NSwag"] = "NSwag",
        ["Refit"] = "Refit",
        ["Swashbuckle"] = "Swashbuckle",
        ["System"] = "System",
        ["TXSpell"] = "TXTextControl",
        ["TXTextControl"] = "TXTextControl",
        ["Unity"] = "Unity",
        ["Vanbreda"] = "Vanbreda",
        ["XUnit"] = "XUnit",
    };
}
