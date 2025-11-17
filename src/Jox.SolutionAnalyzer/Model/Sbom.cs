using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Jox.SolutionAnalyzer.Model;

internal class Sbom() : DbContext("name=Sbom")
{
    public virtual DbSet<Repository> Repositories { get; set; } = null!;
    public virtual DbSet<Solution> Solutions { get; set; } = null!;
    public virtual DbSet<MSBuildProject> MSBuildProjects { get; set; } = null!;
    public virtual DbSet<NonMsBuildProject> NonMsBuildProjects { get; set; } = null!;
    public virtual DbSet<PackageReference> PackageReferences { get; set; } = null!;
    public virtual DbSet<ProjectReference> ProjectReferences { get; set; } = null!;
    public virtual DbSet<AssemblyReference> AssemblyReferences { get; set; } = null!;


    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Properties<string>().Configure(p => p.HasMaxLength(p.ClrPropertyInfo.Name == "ParseIssue" ? 4096 : 255));
        modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
    }

    public void AddRepository(Repository repository)
    {
        Repositories.Add(repository);

        foreach (var sln in repository.Solutions)
        {
            Solutions.Add(sln);
            foreach (var proj in sln.NonMSBuildProjects)
            {
                NonMsBuildProjects.Add(proj);
            }
        }
        foreach (var proj in repository.Projects)
        {
            MSBuildProjects.Add(proj);
            foreach (var projectRef in proj.ProjectReferences)
            {
                ProjectReferences.Add(projectRef);
            }
            foreach (var assemblyRef in proj.AssemblyReferences)
            {
                AssemblyReferences.Add(assemblyRef);
            }
            foreach (var packageRef in proj.PackageReferences)
            {
                PackageReferences.Add(packageRef);
            }
        }
    }
}
