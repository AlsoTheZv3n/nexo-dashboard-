using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class ScheduledExecutionConfiguration : IEntityTypeConfiguration<ScheduledExecution>
{
    public void Configure(EntityTypeBuilder<ScheduledExecution> b)
    {
        b.ToTable("scheduled_executions");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id");
        b.Property(s => s.ScriptId).HasColumnName("script_id").IsRequired();
        b.Property(s => s.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(s => s.CronExpression).HasColumnName("cron_expression").HasMaxLength(128).IsRequired();
        b.Property(s => s.ParametersJson).HasColumnName("parameters_json").HasColumnType("jsonb").IsRequired();
        b.Property(s => s.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(s => s.CreatedByUserId).HasColumnName("created_by").IsRequired();
        b.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(s => s.LastRunAt).HasColumnName("last_run_at");
        b.Property(s => s.NextRunAt).HasColumnName("next_run_at");
        b.HasIndex(s => new { s.IsActive, s.NextRunAt });
    }
}
