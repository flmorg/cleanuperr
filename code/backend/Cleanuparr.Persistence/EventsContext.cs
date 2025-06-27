using Cleanuparr.Persistence.Converters;
using Cleanuparr.Persistence.Models.Events;
using Cleanuparr.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cleanuparr.Persistence;

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
                var enumType = property.PropertyType.IsEnum 
                    ? property.PropertyType 
                    : property.PropertyType.GetGenericArguments()[0];

                var converterType = typeof(LowercaseEnumConverter<>).MakeGenericType(enumType);
                var converter = Activator.CreateInstance(converterType);

                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion((ValueConverter)converter);
            }
        }
    }
    
    public static string GetLikePattern(string input)
    {
        input = input.Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
        
        return $"%{input}%";
    }
} 