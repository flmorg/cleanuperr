using Common.Configuration;
using Data.Models.Configuration.Arr;
using Data.Models.Configuration.DownloadCleaner;
using Data.Models.Configuration.General;
using Data.Models.Configuration.Notification;
using Data.Models.Configuration.QueueCleaner;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Data;

/// <summary>
/// Database context for configuration data
/// </summary>
public class DataContext : DbContext
{
    public static SemaphoreSlim Lock { get; } = new(1, 1);
    
    public DbSet<GeneralConfig> GeneralConfigs { get; set; }
    
    public DbSet<DownloadClientConfig> DownloadClients { get; set; }
    
    public DbSet<QueueCleanerConfig> QueueCleanerConfigs { get; set; }
    
    public DbSet<DownloadCleanerConfig> DownloadCleanerConfigs { get; set; }
    
    public DbSet<ArrConfig> ArrConfigs { get; set; }
    
    public DbSet<ArrInstance> ArrInstances { get; set; }
    
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
    }
} 