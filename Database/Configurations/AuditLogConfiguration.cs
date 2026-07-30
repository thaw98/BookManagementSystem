using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityDisplayName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.ActorEmail).HasMaxLength(150);
        builder.Property(x => x.ChangeDetailsJson).HasColumnType("longtext").IsRequired();
        builder.HasIndex(x => new { x.OccurredAtUtc, x.Id });
        builder.HasIndex(x => new { x.EntityType, x.Action });
        builder.HasIndex(x => x.ActorUserId);
    }
}
