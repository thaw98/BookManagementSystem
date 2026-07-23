using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Auth;
using Shared.Constants;

namespace BookManagementSystem.Domain.Seeding;

public sealed class DbSeeder(AppDbContext db, IPasswordHasher passwordHasher, ILogger<DbSeeder> logger) : IDbSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = "admin@bms.com";
        var exists = await db.Users.AnyAsync(x => x.Email == email, cancellationToken);

        if (!exists)
        {
            db.Users.Add(new User
            {
                RoleId = RoleNames.AdminId,
                Email = email,
                PasswordHash = passwordHasher.HashPassword("password123"),
                IsActive = true
            });
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded default Admin account.");
        }
    }
}
