using Contracts.Notification;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constants;

namespace BookManagementSystem.Domain.Features.NotificationFeature;

public sealed class NotificationWorkerOptions
{
    public int ScanIntervalMinutes { get; set; } = 5;
    public int StartupDelaySeconds { get; set; } = 5;
}

public sealed class NotificationScanner(
    AppDbContext db,
    INotificationDispatcher dispatcher,
    ILogger<NotificationScanner> logger)
{
    public async Task<int> ScanAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var records = await db.BookBorrowRecords.AsNoTracking()
            .Where(x => x.ReturnedAt == null && x.DueAt <= now.AddHours(24))
            .Select(x => new { x.Id, x.UserId, MemberName = x.User.FullName, x.DueAt, BookTitle = x.Book.Title })
            .ToListAsync(cancellationToken);
        if (records.Count == 0) return 0;

        var librarians = await db.Users.AsNoTracking()
            .Where(x => x.IsActive && x.Role.Name == RoleNames.Librarian)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        var recordIds = records.Select(x => x.Id).ToArray();
        var existing = (await db.Notifications.AsNoTracking()
            .Where(x => recordIds.Contains(x.BorrowRecordId) && (x.Type == "DueSoon" || x.Type == "Overdue"))
            .Select(x => new { x.RecipientUserId, x.BorrowRecordId, x.Type }).ToListAsync(cancellationToken))
            .Select(x => (x.RecipientUserId, x.BorrowRecordId, x.Type)).ToHashSet();
        var added = new List<Notification>();

        void Add(long recipient, long recordId, string type, string title, string message)
        {
            if (!existing.Add((recipient, recordId, type))) return;
            var item = new Notification { RecipientUserId = recipient, BorrowRecordId = recordId,
                Type = type, Title = title, Message = message };
            db.Notifications.Add(item); added.Add(item);
        }

        foreach (var record in records)
        {
            var due = NotificationDateTime.Format(record.DueAt);
            if (record.DueAt > now)
            {
                Add(record.UserId, record.Id, "DueSoon", "Book due soon", $"“{record.BookTitle}” is due on {due}.");
                continue;
            }
            Add(record.UserId, record.Id, "Overdue", "Book overdue",
                $"“{record.BookTitle}” was due on {due}. Please return it as soon as possible.");
            foreach (var librarianId in librarians)
                Add(librarianId, record.Id, "Overdue", "Book overdue",
                    $"{record.MemberName} has not returned “{record.BookTitle}”. Due: {due}.");
        }

        if (added.Count == 0) return 0;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex)
        {
            logger.LogInformation(ex, "A concurrent notification scan won the unique-key race; the next scan will reconcile remaining notifications.");
            foreach (var item in added) db.Entry(item).State = EntityState.Detached;
            return 0;
        }

        try
        {
            await dispatcher.DispatchAsync(added.Select(x => (x.RecipientUserId, NotificationService.Map(x))).ToArray(), cancellationToken);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Background notification live delivery failed after persistence."); }
        return added.Count;
    }
}

public sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationWorkerOptions> options,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        try { await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, settings.StartupDelaySeconds)), stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<NotificationScanner>()
                    .ScanAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Notification scan failed; it will be retried."); }
            try { await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, settings.ScanIntervalMinutes)), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
