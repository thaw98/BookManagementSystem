using BookManagementSystem.Domain.Features.AuthFeature;
using BookManagementSystem.Domain.Features.AuditLogFeature;
using BookManagementSystem.Domain.Features.AuthorFeature;
using BookManagementSystem.Domain.Authorization;
using Contracts.AuditLog;
using Contracts.Author;
using Contracts.Auth;
using Database.AppDbContextModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Base;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Xunit;

namespace BookManagementSystem.Tests;

public sealed class AuthAndAuditTests
{
    [Fact]
    public async Task Deleted_account_is_disclosed_only_after_valid_password_and_issues_no_tokens()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var hasher = new PasswordHasher();
        var role = new Role { Name = "Member" };
        var user = new User
        {
            FullName = "Deleted Member",
            Email = "deleted@example.com",
            PasswordHash = hasher.HashPassword("correct"),
            Role = role,
            IsActive = true
        };
        fixture.Db.Add(user);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.Remove(user);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();

        var tokens = new RecordingTokenService();
        var service = new AuthService(fixture.Db, hasher, tokens);

        var wrong = await service.LoginAsync(
            new LoginRequest { Email = user.Email, Password = "wrong" }, default);
        var deleted = await service.LoginAsync(
            new LoginRequest { Email = user.Email, Password = "correct" }, default);

        Assert.Equal("Invalid email or password.", wrong.Message);
        Assert.Null(wrong.Code);
        Assert.Equal(AuthFailureCodes.DeletedAccount, deleted.Code);
        Assert.Equal(0, tokens.AccessTokensCreated);
        Assert.Empty(await fixture.Db.RefreshTokens.ToListAsync());
    }

    [Fact]
    public async Task Active_account_login_succeeds_and_issues_token_pair()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var hasher = new PasswordHasher();
        var user = new User
        {
            FullName = "Active Member",
            Email = "active@example.com",
            PasswordHash = hasher.HashPassword("correct"),
            Role = new Role { Name = "Member" },
            IsActive = true
        };
        fixture.Db.Add(user);
        await fixture.Db.SaveChangesAsync();
        var tokens = new RecordingTokenService();

        var result = await new AuthService(fixture.Db, hasher, tokens).LoginAsync(
            new LoginRequest { Email = user.Email, Password = "correct" }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active Member", result.Data!.FullName);
        Assert.Equal(1, tokens.AccessTokensCreated);
        Assert.Single(await fixture.Db.RefreshTokens.ToListAsync());
    }

    [Fact]
    public async Task Every_supported_entity_gets_create_update_and_delete_events()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var role = new Role { Name = "Role A" };
        var user = new User { FullName = "User A", Email = "a@example.com", PasswordHash = "hash", Role = role };
        var author = new Author { Name = "Author A" };
        var category = new Category { Name = "Category A" };
        var book = new Book { Title = "Book A", Author = author, Category = category, TotalCopies = 1, AvailableCopies = 1 };
        var borrow = new BookBorrowRecord
        {
            User = user, Book = book, BorrowedAt = DateTime.UtcNow,
            DueAt = DateTime.UtcNow.AddDays(7)
        };
        fixture.Db.AddRange(role, user, author, category, book, borrow);
        await fixture.Db.SaveChangesAsync();

        role.Name = "Role B";
        user.FullName = "User B";
        author.Name = "Author B";
        category.Name = "Category B";
        book.Title = "Book B";
        borrow.ReturnedAt = DateTime.UtcNow;
        await fixture.Db.SaveChangesAsync();

        fixture.Db.RemoveRange(borrow, book, category, author, user, role);
        await fixture.Db.SaveChangesAsync();

        var logs = await fixture.Db.AuditLogs.AsNoTracking().ToListAsync();
        foreach (var entityType in new[] { "Role", "Account", "Author", "Category", "Book", "Borrow Record" })
        {
            Assert.Contains(logs, x => x.EntityType == entityType && x.Action == AuditAction.Created);
            Assert.Contains(logs, x => x.EntityType == entityType && x.Action == AuditAction.Updated);
            Assert.Contains(logs, x => x.EntityType == entityType && x.Action == AuditAction.Deleted);
        }
    }

    [Fact]
    public async Task Snapshots_are_immutable_and_secret_values_never_enter_change_json()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var user = new User
        {
            FullName = "Original Name", Email = "snapshot@example.com",
            PasswordHash = "old-secret", Role = new Role { Name = "Member" }
        };
        fixture.Db.Add(user);
        await fixture.Db.SaveChangesAsync();
        user.FullName = "Renamed";
        user.PasswordHash = "new-secret";
        await fixture.Db.SaveChangesAsync();

        var logs = await fixture.Db.AuditLogs.Where(x => x.EntityType == "Account")
            .OrderBy(x => x.Id).ToListAsync();

        Assert.Equal("Original Name", logs[0].EntityDisplayName);
        Assert.DoesNotContain("old-secret", logs[1].ChangeDetailsJson);
        Assert.DoesNotContain("new-secret", logs[1].ChangeDetailsJson);
        Assert.Contains("\"Password\":{\"changed\":true}", logs[1].ChangeDetailsJson);
    }

    [Fact]
    public async Task Entity_write_rolls_back_when_audit_insert_fails()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        await fixture.Connection.ExecuteNonQueryAsync(
            "CREATE TRIGGER reject_audit BEFORE INSERT ON AuditLogs BEGIN SELECT RAISE(ABORT, 'audit rejected'); END;");
        fixture.Db.Authors.Add(new Author { Name = "Must Roll Back" });

        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());

        fixture.Db.ChangeTracker.Clear();
        Assert.False(await fixture.Db.Authors.AnyAsync());
    }

    [Fact]
    public async Task Rejected_operation_produces_no_entity_or_audit_event()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var result = await new AuthorService(fixture.Db).CreateAsync(
            new CreateAuthorRequest { Name = "   " }, default);

        Assert.False(result.IsSuccess);
        Assert.Empty(await fixture.Db.Authors.ToListAsync());
        Assert.Empty(await fixture.Db.AuditLogs.ToListAsync());
    }

    [Fact]
    public void Audit_controller_requires_the_admin_policy()
    {
        var authorization = typeof(AuditLogController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single(x => x.Policy == PermissionProvider.AdminOnly);

        Assert.NotNull(authorization);
        Assert.Equal(PermissionProvider.AdminOnly, authorization.Policy);
    }

    [Fact]
    public async Task Audit_query_has_stable_paging_filters_and_Myanmar_day_boundaries()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var sameTime = new DateTime(2026, 7, 28, 8, 0, 0, DateTimeKind.Utc);
        fixture.Db.AuditLogs.AddRange(
            NewLog("Before", sameTime.AddDays(-1), 7, AuditAction.Created),
            NewLog("Book One", sameTime, 7, AuditAction.Updated),
            NewLog("Book Two", sameTime, 7, AuditAction.Updated),
            NewLog("After", new DateTime(2026, 7, 28, 18, 0, 0, DateTimeKind.Utc), 7, AuditAction.Updated));
        await fixture.Db.SaveChangesAsync();
        var service = new AuditLogService(fixture.Db);

        var firstPage = await service.GetPagedAsync(new AuditLogFilterRequest
        {
            Page = 1,
            PageSize = 1,
            Search = "Book",
            EntityType = "Book",
            Action = "Updated",
            Actor = "7",
            FromDateLocal = new DateTime(2026, 7, 28),
            ToDateLocal = new DateTime(2026, 7, 28)
        }, default);
        var secondPage = await service.GetPagedAsync(new AuditLogFilterRequest
        {
            Page = 2,
            PageSize = 1,
            Search = "Book",
            EntityType = "Book",
            Action = "Updated",
            Actor = "7",
            FromDateLocal = new DateTime(2026, 7, 28),
            ToDateLocal = new DateTime(2026, 7, 28)
        }, default);

        Assert.Equal(2, firstPage.Data!.TotalCount);
        Assert.Equal("Book Two", firstPage.Data.Items.Single().EntityDisplayName);
        Assert.Equal("Book One", secondPage.Data!.Items.Single().EntityDisplayName);
    }

    private static AuditLog NewLog(
        string displayName, DateTime occurredAt, long actorId, AuditAction action) =>
        new()
        {
            EntityType = "Book",
            EntityId = 1,
            EntityDisplayName = displayName,
            Action = action,
            ActorUserId = actorId,
            ActorDisplayName = "Deleted Admin",
            ActorEmail = "deleted-admin@example.com",
            OccurredAtUtc = occurredAt,
            ChangeDetailsJson = "{}"
        };

    private sealed class RecordingTokenService : IJwtTokenService
    {
        public int AccessTokensCreated { get; private set; }
        public AccessTokenResult CreateAccessToken(long userId, string email, string roleName, string fullName)
        {
            AccessTokensCreated++;
            return new AccessTokenResult("access", DateTime.UtcNow.AddMinutes(5));
        }
        public RefreshTokenResult CreateRefreshToken() =>
            new("refresh", "refresh-hash", DateTime.UtcNow.AddDays(1));
        public string HashRefreshToken(string rawToken) => rawToken;
    }

    private sealed class ActorContext : IBaseService
    {
        public long? UserId => 99;
        public string? UserDisplayName => "Admin One";
        public string? UserEmail => "admin@example.com";
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        public SqliteConnection Connection { get; }
        public AppDbContext Db { get; }

        private TestDatabase(SqliteConnection connection, AppDbContext db)
        {
            Connection = connection;
            Db = db;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection).Options;
            var db = new AppDbContext(options, new ActorContext());
            await db.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}

internal static class SqliteExtensions
{
    public static async Task ExecuteNonQueryAsync(this SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
