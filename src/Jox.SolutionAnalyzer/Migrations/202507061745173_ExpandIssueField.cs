namespace Jox.SolutionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ExpandIssueField : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.MSBuildProjects", "ParseIssue", c => c.String());
            AlterColumn("dbo.Repositories", "ParseIssue", c => c.String());
            AlterColumn("dbo.Solutions", "ParseIssue", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Solutions", "ParseIssue", c => c.String(maxLength: 255));
            AlterColumn("dbo.Repositories", "ParseIssue", c => c.String(maxLength: 255));
            AlterColumn("dbo.MSBuildProjects", "ParseIssue", c => c.String(maxLength: 255));
        }
    }
}
