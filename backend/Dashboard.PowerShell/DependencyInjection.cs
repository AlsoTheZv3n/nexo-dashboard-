using Dashboard.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.PowerShell;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardPowerShell(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PowerShellOptions>(configuration.GetSection(PowerShellOptions.SectionName));
        services.AddSingleton<IPowerShellExecutor, PowerShellExecutor>();
        services.AddSingleton<ExecutionRunner>();
        return services;
    }
}
