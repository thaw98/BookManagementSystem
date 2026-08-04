using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.ReadAt).IsRequired(false);
        builder.HasOne(x => x.RecipientUser).WithMany(x => x.Notifications)
            .HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BorrowRecord).WithMany(x => x.Notifications)
            .HasForeignKey(x => x.BorrowRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.RecipientUserId, x.ReadAt, x.CreatedAt });
        builder.HasIndex(x => new { x.RecipientUserId, x.BorrowRecordId, x.Type }).IsUnique();
    }
}
