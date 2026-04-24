using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> b)
    {
        b.ToTable("audit_log");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.UserId).HasColumnName("user_id");
        b.Property(e => e.Action).HasColumnName("action").HasMaxLength(128).IsRequired();
        b.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(64);
        b.Property(e => e.TargetId).HasColumnName("target_id").HasMaxLength(64);
        b.Property(e => e.DetailsJson).HasColumnName("details_json").HasColumnType("jsonb");
        b.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        b.Property(e => e.Timestamp).HasColumnName("timestamp").IsRequired();
        b.HasIndex(e => e.Timestamp);
    }
}
