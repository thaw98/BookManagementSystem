using BookManagementSystem.Domain.Features.UserFeature;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Base;

namespace BookManagementSystem.Domain.Tests;

internal static class ServiceTestContext
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static UserService CreateUserService(AppDbContext db) =>
        new(db, new PasswordHasher(), new TestBaseService());

    private sealed class TestBaseService : IBaseService
    {
        public long? UserId => null;
    }
}
