using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class PsExecutionConfiguration : IEntityTypeConfiguration<PsExecution>
{
    public void Configure(EntityTypeBuilder<PsExecution> b)
    {
        b.ToTable("ps_executions");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.ScriptId).HasColumnName("script_id").IsRequired();
        b.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        b.Property(e => e.ParametersJson).HasColumnName("parameters_json").HasColumnType("jsonb").IsRequired();
        b.Property(e => e.Stdout).HasColumnName("stdout");
        b.Property(e => e.Stderr).HasColumnName("stderr");
        b.Property(e => e.ExitCode).HasColumnName("exit_code");
        b.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(e => e.StartedAt).HasColumnName("started_at");
        b.Property(e => e.CompletedAt).HasColumnName("completed_at");
        b.HasIndex(e => e.ScriptId);
        b.HasIndex(e => new { e.Status, e.CreatedAt });
    }
}
