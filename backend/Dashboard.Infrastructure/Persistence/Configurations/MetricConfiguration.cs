using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class MetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> b)
    {
        b.ToTable("metrics");
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasColumnName("id");
        b.Property(m => m.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        b.Property(m => m.Value).HasColumnName("value").IsRequired();
        b.Property(m => m.TagsJson).HasColumnName("tags_json").HasColumnType("jsonb");
        b.Property(m => m.Timestamp).HasColumnName("timestamp").IsRequired();
        b.HasIndex(m => new { m.Key, m.Timestamp });
    }
}
