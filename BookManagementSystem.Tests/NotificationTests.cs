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
        Assert.Contains(NotificationDateTime.Format(result.Data!.DueAt), notification.Message);
        Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}T", notification.Message);
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
        Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}T", row.Message);
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
        Assert.Contains(rows, x => x.Type == "DueSoon" &&
            x.Message.Contains("04/08/2026 10:30 PM") &&
            !System.Text.RegularExpressions.Regex.IsMatch(x.Message, @"\d{4}-\d{2}-\d{2}T"));
        Assert.All(rows.Where(x => x.Type == "Overdue"), x =>
        {
            Assert.Contains("04/08/2026 10:29 AM", x.Message);
            Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}T", x.Message);
        });
    }

    [Fact]
    public async Task Paged_history_is_recipient_scoped_includes_read_state_and_has_stable_ordering()
    {
        await using var f = await Fixture.CreateAsync();
        var first = f.User("First", RoleNames.LibraryMember, true);
        var second = f.User("Second", RoleNames.LibraryMember, true);
        var record = f.Record(first, f.Book("History", 1), DateTime.UtcNow.AddDays(1));
        await f.Db.SaveChangesAsync();
        var readAt = DateTime.UtcNow;
        var firstItems = new[]
        {
            new Notification { RecipientUserId = first.Id, BorrowRecordId = record.Id, Type = "One", Title = "One", Message = "One" },
            new Notification { RecipientUserId = first.Id, BorrowRecordId = record.Id, Type = "Two", Title = "Two", Message = "Two", ReadAt = readAt },
            new Notification { RecipientUserId = first.Id, BorrowRecordId = record.Id, Type = "Three", Title = "Three", Message = "Three" },
            new Notification { RecipientUserId = first.Id, BorrowRecordId = record.Id, Type = "Four", Title = "Four", Message = "Four" }
        };
        var other = new Notification
        {
            RecipientUserId = second.Id, BorrowRecordId = record.Id, Type = "Other",
            Title = "Other user", Message = "Must not be exposed"
        };
        f.Db.Notifications.AddRange(firstItems);
        f.Db.Notifications.Add(other);
        await f.Db.SaveChangesAsync();

        var older = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
        var newer = older.AddDays(1);
        await f.Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Notifications SET CreatedAt = {older} WHERE Id IN ({firstItems[0].Id}, {firstItems[3].Id})");
        await f.Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Notifications SET CreatedAt = {newer} WHERE Id IN ({firstItems[1].Id}, {firstItems[2].Id}, {other.Id})");

        f.Base.UserIdValue = first.Id;
        var service = f.NotificationService();
        var firstPage = await service.GetPagedAsync(new() { Page = 1, PageSize = 2 }, default);
        var secondPage = await service.GetPagedAsync(new() { Page = 2, PageSize = 2 }, default);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(4, firstPage.Data!.TotalCount);
        Assert.Equal(1, firstPage.Data.Page);
        Assert.Equal(2, firstPage.Data.PageSize);
        Assert.Equal(2, firstPage.Data.TotalPages);
        Assert.Equal(new[] { firstItems[2].Id, firstItems[1].Id }, firstPage.Data.Items.Select(x => x.Id));
        Assert.Contains(firstPage.Data.Items, x => x.ReadAt is null);
        Assert.Contains(firstPage.Data.Items, x => x.ReadAt is not null);
        Assert.Equal(2, secondPage.Data!.Page);
        Assert.Equal(new[] { firstItems[3].Id, firstItems[0].Id }, secondPage.Data.Items.Select(x => x.Id));
        Assert.DoesNotContain(firstPage.Data.Items.Concat(secondPage.Data.Items), x => x.Id == other.Id);
    }

    [Fact]
    public async Task Inbox_includes_read_and_unread_returns_five_newest_for_recipient_and_counts_all_unread()
    {
        await using var f = await Fixture.CreateAsync();
        var recipient = f.User("Inbox recipient", RoleNames.LibraryMember, true);
        var otherUser = f.User("Other recipient", RoleNames.LibraryMember, true);
        var record = f.Record(recipient, f.Book("Inbox ordering", 1), DateTime.UtcNow.AddDays(1));
        await f.Db.SaveChangesAsync();
        var readAt = new DateTime(2026, 8, 4, 1, 0, 0, DateTimeKind.Utc);
        var recipientItems = Enumerable.Range(0, 7).Select(index => new Notification
        {
            RecipientUserId = recipient.Id,
            BorrowRecordId = record.Id,
            Type = $"Inbox{index}",
            Title = $"Notification {index}",
            Message = $"Message {index}",
            ReadAt = index % 2 == 1 ? readAt : null
        }).ToArray();
        var otherItem = new Notification
        {
            RecipientUserId = otherUser.Id,
            BorrowRecordId = record.Id,
            Type = "OtherInbox",
            Title = "Other user's notification",
            Message = "Must not be exposed"
        };
        f.Db.Notifications.AddRange(recipientItems);
        f.Db.Notifications.Add(otherItem);
        await f.Db.SaveChangesAsync();

        var sameCreatedAt = new DateTime(2026, 8, 4, 2, 0, 0, DateTimeKind.Utc);
        await f.Db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Notifications SET CreatedAt = {sameCreatedAt} WHERE RecipientUserId = {recipient.Id}");

        f.Base.UserIdValue = recipient.Id;
        var result = await f.NotificationService().GetInboxAsync(default);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Data!.UnreadCount);
        Assert.Equal(5, result.Data.Notifications.Count);
        Assert.Equal(recipientItems.Reverse().Take(5).Select(x => x.Id),
            result.Data.Notifications.Select(x => x.Id));
        Assert.Contains(result.Data.Notifications, x => x.ReadAt is null);
        Assert.Contains(result.Data.Notifications, x => x.ReadAt is not null);
        Assert.DoesNotContain(result.Data.Notifications, x => x.Id == otherItem.Id);
        Assert.DoesNotContain(result.Data.Notifications, x => x.Id == recipientItems[0].Id);
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
