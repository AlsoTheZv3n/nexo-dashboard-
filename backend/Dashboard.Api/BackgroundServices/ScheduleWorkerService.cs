using Dashboard.Core.Abstractions;
using Dashboard.Core.Entities;
using Dashboard.PowerShell;
using Microsoft.Extensions.DependencyInjection;
using NCrontab;

namespace Dashboard.Api.BackgroundServices;

/// <summary>
/// Polls every 30 seconds for schedules whose NextRunAt is due, creates a
/// PsExecution row, kicks the ExecutionRunner off as fire-and-forget, and
/// recomputes NextRunAt from the cron expression.
/// </summary>
public sealed class ScheduleWorkerService(
    IServiceScopeFactory scopes,
    ExecutionRunner runner,
    IHostApplicationLifetime lifetime,
    ILogger<ScheduleWorkerService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ScheduleWorker tick failed");
            }
            try { await Task.Delay(PollInterval, stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var schedules = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
        var executions = scope.ServiceProvider.GetRequiredService<IExecutionRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var now = clock.UtcNow;
        var due = await schedules.GetDueAsync(now, ct);
        foreach (var schedule in due)
        {
            var exec = new PsExecution(schedule.ScriptId, schedule.CreatedByUserId, schedule.ParametersJson);
            await executions.AddAsync(exec, ct);
            _ = Task.Run(() => runner.RunAsync(exec.Id, lifetime.ApplicationStopping), CancellationToken.None);

            var cron = CrontabSchedule.Parse(schedule.CronExpression);
            var next = new DateTimeOffset(cron.GetNextOccurrence(now.UtcDateTime), TimeSpan.Zero);
            schedule.RecordRun(now, next);
            await schedules.UpdateAsync(schedule, ct);
        }
    }
}
