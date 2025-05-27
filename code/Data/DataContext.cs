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
} 