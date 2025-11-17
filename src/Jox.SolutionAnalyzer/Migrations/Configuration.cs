namespace Jox.SolutionAnalyzer.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Jox.SolutionAnalyzer.Model.Sbom>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(Jox.SolutionAnalyzer.Model.Sbom context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
