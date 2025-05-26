using Common.Helpers;
using Data.Models.Events;
using Microsoft.EntityFrameworkCore;

namespace Data;

/// <summary>
/// Database context for events
/// </summary>
public class DataContext : DbContext
{
    public DbSet<AppEvent> Events { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(ConfigurationPathProvider.GetSettingsPath(), "state.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Additional configuration if needed
        modelBuilder.Entity<AppEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CorrelationId).HasMaxLength(50);
        });
    }
} 