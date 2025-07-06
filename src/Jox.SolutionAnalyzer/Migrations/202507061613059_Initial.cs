namespace Jox.SolutionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AssemblyReferences",
                c => new
                    {
                        RepositoryId = c.String(nullable: false, maxLength: 255),
                        ProjectFileRelativePath = c.String(nullable: false, maxLength: 255),
                        AssemblyName = c.String(nullable: false, maxLength: 255),
                        HintPath = c.String(maxLength: 255),
                        RepositoryRelativePath = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => new { t.RepositoryId, t.ProjectFileRelativePath, t.AssemblyName });
            
            CreateTable(
                "dbo.MSBuildProjects",
                c => new
                    {
                        RepositoryId = c.String(nullable: false, maxLength: 255),
                        ProjectFileRelativePath = c.String(nullable: false, maxLength: 255),
                        ProjectName = c.String(maxLength: 255),
                        TargetFrameworkVersion = c.String(maxLength: 255),
                        TargetFramework = c.String(maxLength: 255),
                        TargetFrameworks = c.String(maxLength: 255),
                        PackageId = c.String(maxLength: 255),
                        ParseIssue = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => new { t.RepositoryId, t.ProjectFileRelativePath })
                .ForeignKey("dbo.Repositories", t => t.RepositoryId)
                .Index(t => t.RepositoryId);
            
            CreateTable(
                "dbo.Repositories",
                c => new
                    {
                        RepositoryId = c.String(nullable: false, maxLength: 255),
                        ParseIssue = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.RepositoryId);
            
            CreateTable(
                "dbo.NonMsBuildProjects",
                c => new
                    {
                        NonMsBuildProjectId = c.Int(nullable: false, identity: true),
                        RepositoryId = c.String(maxLength: 255),
                        RelativePath = c.String(maxLength: 255),
                        ProjectName = c.String(maxLength: 255),
                        ProjectType = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.NonMsBuildProjectId);
            
            CreateTable(
                "dbo.PackageReferences",
                c => new
                    {
                        RepositoryId = c.String(nullable: false, maxLength: 255),
                        ProjectFileRelativePath = c.String(nullable: false, maxLength: 255),
                        PackageName = c.String(nullable: false, maxLength: 255),
                        PackageVersion = c.String(maxLength: 255),
                        FromPackagesConfig = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.RepositoryId, t.ProjectFileRelativePath, t.PackageName });
            
            CreateTable(
                "dbo.ProjectReferences",
                c => new
                    {
                        RepositoryId = c.String(nullable: false, maxLength: 255),
                        ProjectFileRelativePath = c.String(nullable: false, maxLength: 255),
                        ReferencedProjectFileRelativePath = c.String(nullable: false, maxLength: 255),
                    })
                .PrimaryKey(t => new { t.RepositoryId, t.ProjectFileRelativePath, t.ReferencedProjectFileRelativePath })
                .ForeignKey("dbo.MSBuildProjects", t => new { t.RepositoryId, t.ProjectFileRelativePath })
                .ForeignKey("dbo.Repositories", t => t.RepositoryId)
                .Index(t => new { t.RepositoryId, t.ProjectFileRelativePath });
            
            CreateTable(
                "dbo.Solutions",
                c => new
                    {
                        RepositoryId = c.String(nullable: false, maxLength: 255),
                        SolutionFileRelativePath = c.String(nullable: false, maxLength: 255),
                        ParseIssue = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => new { t.RepositoryId, t.SolutionFileRelativePath })
                .ForeignKey("dbo.Repositories", t => t.RepositoryId)
                .Index(t => t.RepositoryId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Solutions", "RepositoryId", "dbo.Repositories");
            DropForeignKey("dbo.ProjectReferences", "RepositoryId", "dbo.Repositories");
            DropForeignKey("dbo.ProjectReferences", new[] { "RepositoryId", "ProjectFileRelativePath" }, "dbo.MSBuildProjects");
            DropForeignKey("dbo.MSBuildProjects", "RepositoryId", "dbo.Repositories");
            DropIndex("dbo.Solutions", new[] { "RepositoryId" });
            DropIndex("dbo.ProjectReferences", new[] { "RepositoryId", "ProjectFileRelativePath" });
            DropIndex("dbo.MSBuildProjects", new[] { "RepositoryId" });
            DropTable("dbo.Solutions");
            DropTable("dbo.ProjectReferences");
            DropTable("dbo.PackageReferences");
            DropTable("dbo.NonMsBuildProjects");
            DropTable("dbo.Repositories");
            DropTable("dbo.MSBuildProjects");
            DropTable("dbo.AssemblyReferences");
        }
    }
}
