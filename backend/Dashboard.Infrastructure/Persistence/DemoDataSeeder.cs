using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Infrastructure.Persistence;

/// <summary>
/// Opt-in seeder that populates the DB with plausible but clearly-fake demo data so
/// the dashboard is not empty on a fresh install. Idempotent: if any execution row
/// already exists, the seeder assumes the DB is real-world and exits.
///
/// Toggling it off is the only thing required to go "real": set <c>Demo:SeedEnabled=false</c>,
/// wipe demo rows (or start from a migrated-empty DB) and let production events flow in.
/// See docs/MOCK_DATA.md for the full playbook.
/// </summary>
public static class DemoDataSeeder
{
    private const string DemoScriptIdTag = "demo-seed";

    public static async Task SeedAsync(DashboardDbContext db, IClock clock, CancellationToken ct = default)
    {
        // Idempotence guard: non-empty DB → do nothing.
        if (await db.Executions.AnyAsync(ct)) return;

        var scripts = await db.Scripts.AsNoTracking().ToListAsync(ct);
        var admin = await db.Users.AsNoTracking().FirstAsync(u => u.Username == "admin", ct);
        if (scripts.Count == 0) return;   // base seeder hasn't run yet; skip this tick

        var now = clock.UtcNow;

        // ---- 30 days of demo executions (roughly business-hours shaped) ----
        var executions = new List<PsExecution>();
        var random = new Random(42);
        for (var day = 30; day >= 0; day--)
        {
            var date = now.AddDays(-day);
            for (var i = 0; i < random.Next(3, 12); i++)
            {
                var script = scripts[random.Next(scripts.Count)];
                var started = date.AddHours(6 + random.Next(11)).AddMinutes(random.Next(60));
                var completed = started.AddSeconds(random.NextDouble() * 12 + 0.5);
                var status = random.NextDouble() switch
                {
                    < 0.85 => ExecutionStatus.Completed,
                    < 0.95 => ExecutionStatus.Failed,
                    _ => ExecutionStatus.Cancelled,
                };
                var exec = new PsExecution(script.Id, admin.Id, "{}");
                exec.MarkRunning();
                if (status == ExecutionStatus.Completed)
                    exec.MarkCompleted("OK", null, 0);
                else if (status == ExecutionStatus.Failed)
                    exec.MarkCompleted(null, "simulated failure", 1);
                else
                    exec.MarkCancelled();
                // Backdate timestamps via private-setter reflection would fight the domain.
                // Instead: rely on the natural ordering of CreatedAt = now. If docs call for
                // historical dates, a future DemoSeeder v2 can add a small Domain extension.
                executions.Add(exec);
            }
        }
        db.Executions.AddRange(executions);

        // ---- Metrics: executions.completed / .failed + duration_seconds for 30 days ----
        var metrics = new List<Metric>();
        for (var hours = 30 * 24; hours >= 0; hours--)
        {
            var ts = now.AddHours(-hours);
            var completedCount = Math.Max(0, random.Next(0, 6) - (ts.Hour < 6 || ts.Hour > 22 ? 3 : 0));
            var failureRate = random.NextDouble() < 0.1 ? random.Next(0, 2) : 0;

            for (var k = 0; k < completedCount; k++)
                metrics.Add(new Metric("executions.completed", 1, ts, $"{{\"source\":\"{DemoScriptIdTag}\"}}"));
            for (var k = 0; k < failureRate; k++)
                metrics.Add(new Metric("executions.failed", 1, ts, $"{{\"source\":\"{DemoScriptIdTag}\"}}"));
            if (completedCount > 0)
            {
                metrics.Add(new Metric("executions.duration_seconds", 1.0 + random.NextDouble() * 4, ts,
                    $"{{\"source\":\"{DemoScriptIdTag}\"}}"));
            }
            // Host health signals — plausible percentages
            metrics.Add(new Metric("host.cpu.percent", 15 + random.NextDouble() * 40, ts, null));
            metrics.Add(new Metric("host.disk.free_percent", 60 + random.NextDouble() * 20, ts, null));
        }
        db.Metrics.AddRange(metrics);

        // ---- A sample alert rule so the Alerts page isn't empty ----
        db.AlertRules.Add(new AlertRule(
            name: "CPU > 90% for 5 min",
            metricKey: "host.cpu.percent",
            op: AlertOperator.GreaterThan,
            threshold: 90,
            windowMinutes: 5,
            aggregation: AlertAggregation.Avg,
            webhookUrl: null));

        // ---- A sample schedule ----
        db.ScheduledExecutions.Add(new ScheduledExecution(
            scriptId: scripts.First().Id,
            name: "Hourly health check (demo)",
            cronExpression: "0 * * * *",
            parametersJson: "{}",
            createdBy: admin.Id,
            firstNextRun: now.AddHours(1)));

        // ---- A few audit log entries so the Audit page is meaningful ----
        db.AuditLog.AddRange(new[]
        {
            new AuditLogEntry(admin.Id, "auth.login", "User", admin.Id.ToString(), null, "127.0.0.1"),
            new AuditLogEntry(admin.Id, "demo.seed", null, null, "{\"note\":\"demo data installed\"}", null),
        });

        await db.SaveChangesAsync(ct);
    }
}
