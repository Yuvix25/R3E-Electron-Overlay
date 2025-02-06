using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReHUD.Interfaces;

namespace ReHUD.Models.LapData {
    public class LapDataContext : DbContext {
        public LapDataContext() { }
        public LapDataContext(DbContextOptions<LapDataContext> options) : base(options) { }

        public DbSet<LapContext> LapContexts { get; set; }
        public DbSet<TireWearContext> TireWearContexts { get; set; }
        public DbSet<FuelUsageContext> FuelUsageContexts { get; set; }
        public DbSet<Lap> LapDatas { get; set; }
        public DbSet<LapTime> LapTimes { get; set; }
        public DbSet<TireWear> TireWears { get; set; }
        public DbSet<FuelUsage> FuelUsages { get; set; }
        public DbSet<Telemetry> BestLaps { get; set; }

        private static readonly string DATA_FK = "DataId";
        private static readonly string LAP_CONTEXT_FK = "LapContextId";
        private static readonly string BEST_LAP_FK = "BestLapId";
        private static readonly string TIRE_WEAR_CONTEXT_FK = "TireWearContextId";
        private static readonly string FUEL_USAGE_CONTEXT_FK = "FuelUsageContextId";

        private static readonly Expression<Func<LapContext, object?>> LAP_CONTEXT_KEYS = (c) => new {
            c.TrackLayoutId,
            c.CarId,
            c.ClassPerformanceIndex,
            c.TireCompoundFront,
        };
        private static readonly Expression<Func<TireWearContext, object?>> TIRE_WEAR_CONTEXT_KEYS = (c) => new {
            c.TireWearRate,
        };
        private static readonly Expression<Func<FuelUsageContext, object?>> FUEL_USAGE_CONTEXT_KEYS = (c) => new {
            c.FuelUsageRate,
        };


        private static EntityTypeBuilder<C> ConfigureContext<C>(ModelBuilder modelBuilder, Expression<Func<C, object?>> keys) where C : Context {
            var context = modelBuilder.Entity<C>();
            context.HasIndex(keys);

            return context;
        }

        private static EntityTypeBuilder<C> ConfigureContext<C, T>(ModelBuilder modelBuilder, Expression<Func<C, object?>> keys, string foreignKey) where C : Context<T> where T : class, IEntityWithContext<C> {
            var context = ConfigureContext(modelBuilder, keys);
            context.HasMany(c => c.Entries).WithOne(e => e.Context).HasForeignKey(foreignKey).IsRequired();
            return context;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            try {
                ConfigureContext<LapContext, Lap>(modelBuilder, LAP_CONTEXT_KEYS, LAP_CONTEXT_FK);
                ConfigureContext<TireWearContext, TireWear>(modelBuilder, TIRE_WEAR_CONTEXT_KEYS, TIRE_WEAR_CONTEXT_FK);
                ConfigureContext<FuelUsageContext, FuelUsage>(modelBuilder, FUEL_USAGE_CONTEXT_KEYS, FUEL_USAGE_CONTEXT_FK);

                var lapData = modelBuilder.Entity<Lap>();
                lapData.HasOne(l => l.LapTime).WithOne(c => c.Lap).HasForeignKey<LapTime>(DATA_FK).IsRequired();
                lapData.HasOne(l => l.TireWear).WithOne(c => c.Lap).HasForeignKey<TireWear>(DATA_FK).IsRequired(false);
                lapData.HasOne(l => l.FuelUsage).WithOne(c => c.Lap).HasForeignKey<FuelUsage>(DATA_FK).IsRequired(false);
                lapData.HasOne(l => l.Telemetry).WithOne(c => c.Lap).HasForeignKey<Telemetry>(DATA_FK).IsRequired(false);

                var tireWear = modelBuilder.Entity<TireWear>();
                tireWear.OwnsOne(t => t.Value);

                var telemetry = modelBuilder.Entity<Telemetry>();
                telemetry.OwnsOne(t => t.Value);

                var lapContext = modelBuilder.Entity<LapContext>();
                lapContext.HasOne(c => c.BestLap).WithOne().HasForeignKey<LapContext>(BEST_LAP_FK).IsRequired(false);
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.EnableSensitiveDataLogging().UseLazyLoadingProxies().UseSqlite($"Data Source={ILapDataService.DATA_PATH};Mode=ReadWriteCreate");
            }
        }
    }
}