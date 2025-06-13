using Common.Configuration;
using Common.Configuration.Arr;
using Common.Configuration.DownloadCleaner;
using Common.Configuration.General;
using Common.Configuration.Notification;
using Common.Configuration.QueueCleaner;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Data;

/// <summary>
/// Database context for configuration data
/// </summary>
public class DataContext : DbContext
{
    public DbSet<GeneralConfig> GeneralConfigs { get; set; }
    
    public DbSet<DownloadClient> DownloadClients { get; set; }
    
    public DbSet<QueueCleanerConfig> QueueCleanerConfigs { get; set; }
    
    public DbSet<DownloadCleanerConfig> DownloadCleanerConfigs { get; set; }
    
    public DbSet<SonarrConfig> SonarrConfigs { get; set; }
    
    public DbSet<RadarrConfig> RadarrConfigs { get; set; }
    
    public DbSet<LidarrConfig> LidarrConfigs { get; set; }
    
    public DbSet<AppriseConfig> AppriseConfigs { get; set; }
    
    public DbSet<NotifiarrConfig> NotifiarrConfigs { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }
        
        var dbPath = Path.Combine(ConfigurationPathProvider.GetConfigPath(), "cleanuparr.db");
        optionsBuilder
            .UseSqlite($"Data Source={dbPath}")
            .UseLowerCaseNamingConvention()
            .UseSnakeCaseNamingConvention();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QueueCleanerConfig>(entity =>
        {
            entity.ComplexProperty(e => e.FailedImport);
            entity.ComplexProperty(e => e.Stalled);
            entity.ComplexProperty(e => e.Slow);
            entity.ComplexProperty(e => e.ContentBlocker);
        });
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var enumProperties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType.IsEnum || 
                            (p.PropertyType.IsGenericType && 
                             p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                             p.PropertyType.GetGenericArguments()[0].IsEnum));

            foreach (var property in enumProperties)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion<string>();
            }
        }

        modelBuilder.Entity<QueueCleanerConfig>().HasData(new QueueCleanerConfig());
        modelBuilder.Entity<DownloadCleanerConfig>().HasData(new DownloadCleanerConfig());
        modelBuilder.Entity<GeneralConfig>().HasData(new GeneralConfig());
        modelBuilder.Entity<SonarrConfig>().HasData(new SonarrConfig());
        modelBuilder.Entity<RadarrConfig>().HasData(new RadarrConfig());
        modelBuilder.Entity<LidarrConfig>().HasData(new LidarrConfig());
        modelBuilder.Entity<AppriseConfig>().HasData(new AppriseConfig());
        modelBuilder.Entity<NotifiarrConfig>().HasData(new NotifiarrConfig());
    }
} 