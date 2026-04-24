using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasColumnName("id");
        b.Property(u => u.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
        b.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(256).IsRequired();
        b.Property(u => u.Role).HasColumnName("role").HasConversion<int>().IsRequired();
        b.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
        b.HasIndex(u => u.Username).IsUnique();
    }
}
