namespace Funbit.Ets.Telemetry.Server.Models
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class DBContext : DbContext
    {
        // Your context has been configured to use a 'DBContext' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'Funbit.Ets.Telemetry.Server.Models.DBContext' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'DBContext' 
        // connection string in the application configuration file.
        public DBContext()
            : base("name=DBContext")
        {
        }

        public virtual DbSet<FerryEventModel> FerryEventModels { get; set; }
        public virtual DbSet<FineEventModel> FineEventModels { get; set; }
        public virtual DbSet<TrainEventModel> TrainEventModels { get; set; }
        public virtual DbSet<TollgateEventModel> TollgateEventModels { get; set; }
        public virtual DbSet<JobStatus> JobStatuses { get; set; }
    }

    //public class MyEntity
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //}
}