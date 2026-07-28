using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Configurations;

public class BookBorrowRecordConfiguration
    : IEntityTypeConfiguration<BookBorrowRecord>
{
    public void Configure(
        EntityTypeBuilder<BookBorrowRecord> builder)
    {
        builder.ToTable("BookBorrowRecords");

        builder.Property(x => x.BorrowedAt)
            .IsRequired();

        builder.Property(x => x.DueAt)
            .IsRequired();

        builder.Property(x => x.ReturnedAt)
            .IsRequired(false);

        builder.HasOne(x => x.User)
            .WithMany(x => x.BookBorrowRecords)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Book)
            .WithMany(x => x.BorrowRecords)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.BookId);

        builder.HasIndex(x => x.BorrowedAt);

        builder.HasIndex(x => x.ReturnedAt);
    }
}