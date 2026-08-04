using System.Reflection;
using BookManagementSystem.Api.Notifications;
using BookManagementSystem.Domain.Features.BorrowFeature;
using BookManagementSystem.Domain.Features.NotificationFeature;
using Contracts.Borrow;
using Contracts.Notification;
using Database.AppDbContextModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Base;
using Shared.Constants;
using Xunit;

namespace BookManagementSystem.Tests;

public sealed class NotificationTests
{
    [Fact]
    public async Task Borrow_notifies_each_active_librarian_after_persistence_and_rejection_not_at_all()
    {
        await using var f = await Fixture.CreateAsync();
        var active = f.User("Active librarian", RoleNames.Librarian, true);
        _ = f.User("Inactive librarian", RoleNames.Librarian, false);
        var member = f.User("Member One", RoleNames.LibraryMember, true);
        var book = f.Book("Concurrency in Practice", 2);
        await f.Db.SaveChangesAsync();
        var service = f.BorrowService();

        var result = await service.BorrowBookAsync(member.Id, new() { BookId = book.Id }, default);

        Assert.True(result.IsSuccess);
        var notification = Assert.Single(await f.Db.Notifications.ToListAsync());
        Assert.Equal(active.Id, notification.RecipientUserId);
        Assert.Equal("Borrowed", notification.Type);
        Assert.Contains(member.FullName, notification.Message);
        Assert.Contains(book.Title, notification.Message);
        Assert.All(f.Dispatcher.Delivered, x => Assert.True(x.Notification.Id > 0));

        var rejected = await service.BorrowBookAsync(member.Id, new BorrowBookRequest { BookId = 0 }, default);
        Assert.False(rejected.IsSuccess);
        Assert.Single(await f.Db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Return_notification_has_required_content_and_duplicate_return_creates_nothing()
    {
        await using var f = await Fixture.CreateAsync();
        _ = f.User("Librarian", RoleNames.Librarian, true);
        var member = f.User("Member", RoleNames.LibraryMember, true);
        var book = f.Book("Returned Book", 3);
        await f.Db.SaveChangesAsync();
        var service = f.BorrowService();
        var borrowed = await service.BorrowBookAsync(member.Id, new() { BookId = book.Id }, default);

        var returned = await service.ReturnBookAsync(member.Id, borrowed.Data!.BorrowRecordId, default);
        var row = await f.Db.Notifications.SingleAsync(x => x.Type == "Returned");

        Assert.True(returned.IsSuccess);
        Assert.Contains(book.Title, row.Message);
        Assert.Contains("Available copies: 3 of 3", row.Message);
        Assert.Contains(returned.Data!.ReturnedAt.ToString("O"), row.Message);
        var count = await f.Db.Notifications.CountAsync();
        Assert.False((await service.ReturnBookAsync(member.Id, borrowed.Data.BorrowRecordId, default)).IsSuccess);
        Assert.Equal(count, await f.Db.Notifications.CountAsync());
    }

    [Fact]
    public async Task Notification_insert_failure_rolls_back_borrow_and_copy_count()
    {
        await using var f = await Fixture.CreateAsync();
        _ = f.User("Librarian", RoleNames.Librarian, true);
        var member = f.User("Member", RoleNames.LibraryMember, true);
        var book = f.Book("Atomic Book", 1);
        await f.Db.SaveChangesAsync();
        await using (var command = f.Connection.CreateCommand())
        {
            command.CommandText = "CREATE TRIGGER reject_notification BEFORE INSERT ON Notifications BEGIN SELECT RAISE(ABORT, 'rejected'); END;";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            f.BorrowService().BorrowBookAsync(member.Id, new() { BookId = book.Id }, default));
        f.Db.ChangeTracker.Clear();
        Assert.Empty(await f.Db.BookBorrowRecords.ToListAsync());
        Assert.Equal(1, (await f.Db.Books.SingleAsync()).AvailableCopies);
        Assert.Empty(await f.Db.Notifications.ToListAsync());
        Assert.Empty(f.Dispatcher.Delivered);
    }

    [Fact]
    public async Task Due_soon_and_overdue_scans_have_correct_recipients_are_deduplicated_and_skip_returns()
    {
        await using var f = await Fixture.CreateAsync();
        var librarian = f.User("Librarian", RoleNames.Librarian, true);
        var member = f.User("Member", RoleNames.LibraryMember, true);
        var now = new DateTime(2026, 8, 4, 4, 0, 0, DateTimeKind.Utc);
        var dueSoon = f.Record(member, f.Book("Due Soon", 1), now.AddHours(12));
        var overdue = f.Record(member, f.Book("Late", 1), now.AddMinutes(-1));
        _ = f.Record(member, f.Book("Returned", 1), now.AddMinutes(-1), now);
        await f.Db.SaveChangesAsync();

        await f.Scanner().ScanAsync(now);
        await f.Scanner().ScanAsync(now);
        var rows = await f.Db.Notifications.OrderBy(x => x.Type).ToListAsync();

        Assert.Single(rows, x => x.BorrowRecordId == dueSoon.Id && x.Type == "DueSoon" && x.RecipientUserId == member.Id);
        Assert.Equal(2, rows.Count(x => x.BorrowRecordId == overdue.Id && x.Type == "Overdue"));
        Assert.Contains(rows, x => x.BorrowRecordId == overdue.Id && x.RecipientUserId == librarian.Id);
        Assert.DoesNotContain(rows, x => x.Message.Contains("Returned"));
        Assert.Equal(3, rows.Count);
        Assert.Equal(3, f.Dispatcher.Delivered.Count);
    }

    [Fact]
    public async Task Read_operations_are_owner_scoped_idempotent_and_not_audited()
    {
        await using var f = await Fixture.CreateAsync();
        var first = f.User("First", RoleNames.LibraryMember, true);
        var second = f.User("Second", RoleNames.LibraryMember, true);
        var record = f.Record(first, f.Book("Inbox", 1), DateTime.UtcNow.AddDays(1));
        await f.Db.SaveChangesAsync();
        f.Db.Notifications.AddRange(
            new Notification { RecipientUserId = first.Id, BorrowRecordId = record.Id, Type = "DueSoon", Title = "One", Message = "One" },
            new Notification { RecipientUserId = second.Id, BorrowRecordId = record.Id, Type = "DueSoon", Title = "Two", Message = "Two" });
        await f.Db.SaveChangesAsync();
        var auditCount = await f.Db.AuditLogs.CountAsync();
        var firstRow = await f.Db.Notifications.SingleAsync(x => x.RecipientUserId == first.Id);
        f.Base.UserIdValue = first.Id;
        var service = f.NotificationService();

        Assert.True((await service.MarkReadAsync(firstRow.Id, default)).IsSuccess);
        Assert.True((await service.MarkReadAsync(firstRow.Id, default)).IsSuccess);
        Assert.True((await service.MarkAllReadAsync(default)).IsSuccess);
        Assert.Null((await f.Db.Notifications.SingleAsync(x => x.RecipientUserId == second.Id)).ReadAt);
        Assert.Equal(auditCount, await f.Db.AuditLogs.CountAsync());
        f.Base.UserIdValue = second.Id;
        Assert.False((await service.MarkReadAsync(firstRow.Id, default)).IsSuccess);
    }

    [Fact]
    public void Model_and_realtime_endpoints_enforce_unique_index_and_authentication()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(options);
        var entity = db.Model.FindEntityType(typeof(Notification))!;
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual(new[] { "RecipientUserId", "BorrowRecordId", "Type" }));
        Assert.NotNull(typeof(NotificationHub).GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(NotificationController).BaseType!.GetCustomAttribute<AuthorizeAttribute>());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public SqliteConnection Connection { get; }
        public AppDbContext Db { get; }
        public MutableBase Base { get; } = new();
        public FakeDispatcher Dispatcher { get; } = new();
        private Fixture(SqliteConnection connection, AppDbContext db) { Connection = connection; Db = db; }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var baseService = new MutableBase();
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options, baseService);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db) { Base = { UserIdValue = baseService.UserIdValue } };
        }
        public User User(string name, string role, bool active) { var x = new User { FullName = name, Email = $"{Guid.NewGuid():N}@test", PasswordHash = "x", IsActive = active, Role = new Role { Name = role } }; Db.Add(x); return x; }
        public Book Book(string title, int copies) { var x = new Book { Title = title, TotalCopies = copies, AvailableCopies = copies, Author = new Author { Name = $"A{Guid.NewGuid():N}" }, Category = new Category { Name = $"C{Guid.NewGuid():N}" } }; Db.Add(x); return x; }
        public BookBorrowRecord Record(User user, Book book, DateTime due, DateTime? returned = null) { var x = new BookBorrowRecord { User = user, Book = book, BorrowedAt = due.AddDays(-1), DueAt = due, ReturnedAt = returned }; Db.Add(x); return x; }
        public NotificationService NotificationService() => new(Db, Base, Dispatcher, NullLogger<NotificationService>.Instance);
        public BorrowService BorrowService() => new(Db, NotificationService());
        public NotificationScanner Scanner() => new(Db, Dispatcher, NullLogger<NotificationScanner>.Instance);
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }
    }

    private sealed class MutableBase : IBaseService { public long? UserIdValue { get; set; } public long? UserId => UserIdValue; public string? UserDisplayName => "Test"; public string? UserEmail => "test@example.com"; }
    private sealed class FakeDispatcher : INotificationDispatcher
    {
        public List<(long RecipientUserId, NotificationDto Notification)> Delivered { get; } = [];
        public Task DispatchAsync(IReadOnlyCollection<(long RecipientUserId, NotificationDto Notification)> notifications, CancellationToken cancellationToken = default) { Delivered.AddRange(notifications); return Task.CompletedTask; }
    }
}
