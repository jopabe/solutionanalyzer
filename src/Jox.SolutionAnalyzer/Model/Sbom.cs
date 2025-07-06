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
        modelBuilder.Properties<string>().Configure(p => p.HasMaxLength(255));
        modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
    }
}
