using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;

namespace Dashboard.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(DashboardDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (!db.Users.Any())
        {
            db.Users.Add(new User("admin", hasher.Hash("admin"), UserRole.Admin));
            db.Users.Add(new User("operator", hasher.Hash("operator"), UserRole.Operator));
            db.Users.Add(new User("viewer", hasher.Hash("viewer"), UserRole.Viewer));
        }

        if (!db.Scripts.Any())
        {
            db.Scripts.Add(new PsScript(
                name: "Get-SystemHealth",
                description: "Prüft die System-Gesundheit (CPU, RAM, Disk).",
                filePath: "Get-SystemHealth.ps1",
                metaJson: """{"parameters":[{"name":"MinFreeGB","type":"int","default":10,"required":false}]}""",
                scriptHash: "seed-placeholder-1"));
            db.Scripts.Add(new PsScript(
                name: "Get-DiskUsage",
                description: "Liefert Disk-Usage pro Mount.",
                filePath: "Get-DiskUsage.ps1",
                metaJson: """{"parameters":[]}""",
                scriptHash: "seed-placeholder-2"));
            db.Scripts.Add(new PsScript(
                name: "Test-NetworkConnectivity",
                description: "Pingt einen Ziel-Host.",
                filePath: "Test-NetworkConnectivity.ps1",
                metaJson: """{"parameters":[{"name":"Target","type":"string","required":true}]}""",
                scriptHash: "seed-placeholder-3"));
            db.Scripts.Add(new PsScript(
                name: "Collect-Metrics",
                description: "Sammelt Host-Metriken als Bulk-Payload für /api/v1/metrics/bulk.",
                filePath: "Collect-Metrics.ps1",
                metaJson: """{"parameters":[{"name":"ApiBaseUrl","type":"string","required":false},{"name":"AccessToken","type":"string","required":false}]}""",
                scriptHash: "seed-placeholder-4"));
        }

        await db.SaveChangesAsync(ct);
    }
}
