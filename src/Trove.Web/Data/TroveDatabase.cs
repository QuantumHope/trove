using Microsoft.EntityFrameworkCore;

namespace Trove.Web.Data;

/// <summary>
/// Postgres (Trove's schema in CORA's database) when <c>ConnectionStrings:Trove</c> is set; otherwise a SQLite file in
/// <c>App_Data</c>, so the app runs on a laptop and in tests with nothing to set up.
/// </summary>
public static class TroveDatabase
{
    public static IServiceCollection AddTroveDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var postgres = configuration.GetConnectionString("Trove");
        if (!string.IsNullOrWhiteSpace(postgres))
        {
            services.AddDbContext<TroveDbContext>(options => options.UseNpgsql(
                postgres, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TroveDbContext.Schema)));
            return services;
        }

        var file = configuration["Trove:SqlitePath"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "trove.db");
        services.AddDbContext<TroveDbContext>(options => options.UseSqlite($"Data Source={file}"));
        return services;
    }

    /// <summary>Applies migrations on Postgres; creates the SQLite file's tables if it's new.</summary>
    public static async Task PrepareTroveDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TroveDbContext>();
        if (db.Database.IsNpgsql())
        {
            await db.Database.MigrateAsync();
            return;
        }

        if (db.Database.GetDbConnection().DataSource is { Length: > 0 } path && Path.GetDirectoryName(path) is { Length: > 0 } folder)
        {
            Directory.CreateDirectory(folder);
        }

        await db.Database.EnsureCreatedAsync();
    }
}

/// <summary>Lets <c>dotnet ef migrations add</c> build the Postgres model without a database or settings.</summary>
public sealed class TroveDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<TroveDbContext>
{
    public TroveDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<TroveDbContext>()
        .UseNpgsql("Host=localhost;Database=cora", npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TroveDbContext.Schema))
        .Options);
}
