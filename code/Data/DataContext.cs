using Common.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Data;

/// <summary>
/// Database context for configuration data
/// </summary>
public class DataContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(ConfigurationPathProvider.GetConfigPath(), "cleanuparr.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    }
} 