using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class AlertIncidentConfiguration : IEntityTypeConfiguration<AlertIncident>
{
    public void Configure(EntityTypeBuilder<AlertIncident> b)
    {
        b.ToTable("alert_incidents");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).HasColumnName("id");
        b.Property(i => i.RuleId).HasColumnName("rule_id").IsRequired();
        b.Property(i => i.State).HasColumnName("state").HasConversion<int>().IsRequired();
        b.Property(i => i.ObservedValue).HasColumnName("observed_value").IsRequired();
        b.Property(i => i.TriggeredAt).HasColumnName("triggered_at").IsRequired();
        b.Property(i => i.AcknowledgedAt).HasColumnName("acknowledged_at");
        b.Property(i => i.AcknowledgedByUserId).HasColumnName("acknowledged_by");
        b.Property(i => i.ResolvedAt).HasColumnName("resolved_at");
        b.HasIndex(i => new { i.RuleId, i.State });
        b.HasIndex(i => i.TriggeredAt);
    }
}
