using Common.Helpers;
using Data.Models.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Data;

/// <summary>
/// Database context for events
/// </summary>
public class EventsContext : DbContext
{
    public DbSet<AppEvent> Events { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }
        
        var dbPath = Path.Combine(ConfigurationPathProvider.GetConfigPath(), "events.db");
        optionsBuilder
            .UseSqlite($"Data Source={dbPath}")
            .UseLowerCaseNamingConvention()
            .UseSnakeCaseNamingConvention();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppEvent>(entity =>
        {
            entity.Property(e => e.Timestamp)
                .HasConversion(new UtcDateTimeConverter());
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