using System.Collections.Generic;
using System.Linq;
using Dashboard.Core.Abstractions;
using Dashboard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Dashboard.Tests.Integration;

/// <summary>
/// Spins up a PostgreSQL 16 container per test run (shared via IClassFixture within a class).
/// The DbContext registration is replaced to point at the container; JWT options are overridden via in-memory config.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("dashboard_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _db.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "nexo-dashboard-tests",
                ["Jwt:Audience"] = "nexo-dashboard-tests",
                ["Jwt:SigningKey"] = "test_signing_key_min_32_chars_xxxxxxxxxx",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                // Irrelevant once we replace the DbContext below, but keeps any direct IConfiguration reads sane:
                ["ConnectionStrings:Default"] = _db.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace the production DbContextOptions with one pointing at the test container.
            var toRemove = services.Where(s =>
                s.ServiceType == typeof(DbContextOptions<DashboardDbContext>) ||
                s.ServiceType == typeof(DbContextOptions)).ToList();
            foreach (var s in toRemove) services.Remove(s);

            services.AddDbContext<DashboardDbContext>(opt => opt.UseNpgsql(_db.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        await ctx.Database.MigrateAsync();

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(ctx, hasher);
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
