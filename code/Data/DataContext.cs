using System.Globalization;
using Common.Helpers;
using Data.Models.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        modelBuilder.Entity<AppEvent>(entity =>
        {
            entity.Property(e => e.Timestamp)
                .HasConversion(new UtcDateTimeConverter());
        });
    }
    
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        ) {}
    }
    
    public static string GetLikePattern(string input)
    {
        input = input.Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
        
        return $"%{input}%";
    }
} 