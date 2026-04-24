using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> b)
    {
        b.ToTable("alert_rules");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).HasColumnName("id");
        b.Property(r => r.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(r => r.MetricKey).HasColumnName("metric_key").HasMaxLength(128).IsRequired();
        b.Property(r => r.Operator).HasColumnName("operator").HasConversion<int>().IsRequired();
        b.Property(r => r.Threshold).HasColumnName("threshold").IsRequired();
        b.Property(r => r.WindowMinutes).HasColumnName("window_minutes").IsRequired();
        b.Property(r => r.Aggregation).HasColumnName("aggregation").HasConversion<int>().IsRequired();
        b.Property(r => r.WebhookUrl).HasColumnName("webhook_url").HasMaxLength(512);
        b.Property(r => r.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(r => r.LastEvaluatedAt).HasColumnName("last_evaluated_at");
    }
}
