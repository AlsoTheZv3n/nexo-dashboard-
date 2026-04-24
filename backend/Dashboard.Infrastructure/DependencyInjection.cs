using Dashboard.Core.Abstractions;
using Dashboard.Core.Services;
using Dashboard.Infrastructure.Auth;
using Dashboard.Infrastructure.Persistence;
using Dashboard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dashboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<DashboardDbContext>(opt => opt.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IScriptRepository, ScriptRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped<IMetricsRepository, MetricsRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IAlertIncidentRepository, AlertIncidentRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<AuthService>();
        services.AddScoped<AuditService>();

        return services;
    }
}
