using BookManagementSystem.Domain.Features.UserFeature;
using Contracts.User;
using Database.AppDbContextModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Base;
using Shared.Constants;
using Xunit;

namespace BookManagementSystem.Tests;

public sealed class UserPagingTests
{
    [Fact]
    public async Task GetPagedAsync_ProjectsPersistedFullName()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = await AddUserAsync(database.Db, "Ada Lovelace", "ada@example.com");

        var result = await CreateService(database.Db).GetPagedAsync(
            new UserFilterRequest { Email = user.Email },
            CancellationToken.None);

        Assert.NotNull(result.Data);
        var item = Assert.Single(result.Data.Items);
        Assert.Equal("Ada Lovelace", item.FullName);
    }

    [Fact]
    public async Task GetPagedAsync_SortsFullNameAscending()
    {
        await using var database = await TestDatabase.CreateAsync();
        await AddUserAsync(database.Db, "Zelda Adams", "zelda@example.com");
        await AddUserAsync(database.Db, "Ada Lovelace", "ada@example.com");
        await AddUserAsync(database.Db, "Grace Hopper", "grace@example.com");

        var result = await CreateService(database.Db).GetPagedAsync(
            new UserFilterRequest { SortBy = "fullName" },
            CancellationToken.None);

        Assert.NotNull(result.Data);
        Assert.Equal(
            ["Ada Lovelace", "Grace Hopper", "Zelda Adams"],
            result.Data.Items.Select(x => x.FullName));
    }

    [Fact]
    public async Task GetPagedAsync_SortsFullNameDescending_StablyById()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstDuplicate = await AddUserAsync(database.Db, "Grace Hopper", "grace.one@example.com");
        await AddUserAsync(database.Db, "Zelda Adams", "zelda@example.com");
        var secondDuplicate = await AddUserAsync(database.Db, "Grace Hopper", "grace.two@example.com");

        var result = await CreateService(database.Db).GetPagedAsync(
            new UserFilterRequest { SortBy = "fullName", SortDescending = true },
            CancellationToken.None);

        Assert.NotNull(result.Data);
        var items = result.Data.Items;
        Assert.Equal(["Zelda Adams", "Grace Hopper", "Grace Hopper"], items.Select(x => x.FullName));
        Assert.Equal([firstDuplicate.Id, secondDuplicate.Id], items.Skip(1).Select(x => x.Id));
    }

    private static UserService CreateService(AppDbContext db) =>
        new(db, new PasswordHasher(), new ActorContext());

    private static async Task<User> AddUserAsync(AppDbContext db, string fullName, string email)
    {
        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = "hash",
            RoleId = RoleNames.LibraryMemberId,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private sealed class ActorContext : IBaseService
    {
        public long? UserId => 99;
        public string? UserDisplayName => "Admin One";
        public string? UserEmail => "admin@example.com";
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Db { get; }

        private TestDatabase(SqliteConnection connection, AppDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new AppDbContext(options, new ActorContext());
            await db.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
