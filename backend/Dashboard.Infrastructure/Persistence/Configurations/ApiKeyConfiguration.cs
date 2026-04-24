using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("api_keys");
        b.HasKey(k => k.Id);
        b.Property(k => k.Id).HasColumnName("id");
        b.Property(k => k.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(k => k.KeyHash).HasColumnName("key_hash").HasMaxLength(128).IsRequired();
        b.Property(k => k.Prefix).HasColumnName("prefix").HasMaxLength(32).IsRequired();
        b.Property(k => k.CreatedByUserId).HasColumnName("created_by").IsRequired();
        b.Property(k => k.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(k => k.LastUsedAt).HasColumnName("last_used_at");
        b.Property(k => k.ExpiresAt).HasColumnName("expires_at");
        b.Property(k => k.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(k => k.Role).HasColumnName("role").HasConversion<int>().IsRequired();
        b.HasIndex(k => k.KeyHash).IsUnique();
    }
}
