using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Constants;

namespace Database.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(255);
        builder.HasIndex(x => x.Name);

        var stamp = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Role { Id = RoleNames.AdminId, Name = RoleNames.Admin, CreatedAt = stamp, UpdatedAt = stamp },
            new Role { Id = RoleNames.LibrarianId, Name = RoleNames.Librarian, CreatedAt = stamp, UpdatedAt = stamp },
            new Role { Id = RoleNames.LibraryMemberId, Name = RoleNames.LibraryMember, CreatedAt = stamp, UpdatedAt = stamp });
    }
}
