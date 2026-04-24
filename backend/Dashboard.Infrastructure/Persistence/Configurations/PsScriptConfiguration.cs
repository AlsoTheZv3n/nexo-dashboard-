using Dashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class PsScriptConfiguration : IEntityTypeConfiguration<PsScript>
{
    public void Configure(EntityTypeBuilder<PsScript> b)
    {
        b.ToTable("ps_scripts");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id");
        b.Property(s => s.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(s => s.Description).HasColumnName("description").HasMaxLength(512).IsRequired();
        b.Property(s => s.FilePath).HasColumnName("file_path").HasMaxLength(512).IsRequired();
        b.Property(s => s.MetaJson).HasColumnName("meta_json").HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ScriptHash).HasColumnName("script_hash").HasMaxLength(64).IsRequired();
        b.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.HasIndex(s => s.Name).IsUnique();
    }
}
