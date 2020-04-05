namespace Funbit.Ets.Telemetry.Server.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Funbit.Ets.Telemetry.Server.Models.DBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(Funbit.Ets.Telemetry.Server.Models.DBContext context)
        {
            if (!context.JobStatuses.Any())
            {
                context.JobStatuses.AddOrUpdate(
                    new Models.JobStatus { JobDelivered = false, JobStarted = false }
                    );
            }
        }
    }
}
