using Contracts.Notification;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Base;
using Shared.Constants;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.NotificationFeature;

public sealed class NotificationService(
    AppDbContext db,
    IBaseService baseService,
    INotificationDispatcher dispatcher,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<List<Notification>> AddLibrarianNotificationsAsync(BookBorrowRecord record,
        string type, string title, string message, CancellationToken cancellationToken)
    {
        var ids = await db.Users.AsNoTracking()
            .Where(x => x.IsActive && x.Role.Name == RoleNames.Librarian)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        var notifications = ids.Select(id => new Notification
        {
            RecipientUserId = id, BorrowRecord = record, Type = type, Title = title, Message = message
        }).ToList();
        db.Notifications.AddRange(notifications);
        return notifications;
    }

    public async Task DispatchAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken)
    {
        var payloads = notifications.Select(x => (x.RecipientUserId, Map(x))).ToArray();
        if (payloads.Length == 0) return;
        try { await dispatcher.DispatchAsync(payloads, cancellationToken); }
        catch (Exception ex) { logger.LogWarning(ex, "Notification live delivery failed after persistence."); }
    }

    public async Task<Result<NotificationInboxDto>> GetUnreadAsync(CancellationToken cancellationToken)
    {
        if (baseService.UserId is not { } userId) return Result<NotificationInboxDto>.Unauthorized();
        var items = await db.Notifications.AsNoTracking()
            .Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new NotificationDto { Id = x.Id, BorrowRecordId = x.BorrowRecordId,
                Type = x.Type, Title = x.Title, Message = x.Message, CreatedAt = x.CreatedAt, ReadAt = x.ReadAt })
            .ToListAsync(cancellationToken);
        return Result<NotificationInboxDto>.Success(new() { UnreadCount = items.Count, Notifications = items });
    }

    public async Task<Result<OffsetPagedResult<NotificationDto>>> GetPagedAsync(
        OffsetPagedRequest request, CancellationToken cancellationToken)
    {
        if (baseService.UserId is not { } userId)
            return Result<OffsetPagedResult<NotificationDto>>.Unauthorized();

        var page = await Pagination.OffsetPagination.CreateAsync(
            db.Notifications.AsNoTracking().Where(x => x.RecipientUserId == userId),
            request,
            source => source.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
            x => new NotificationDto
            {
                Id = x.Id,
                BorrowRecordId = x.BorrowRecordId,
                Type = x.Type,
                Title = x.Title,
                Message = x.Message,
                CreatedAt = x.CreatedAt,
                ReadAt = x.ReadAt
            },
            cancellationToken);

        return Result<OffsetPagedResult<NotificationDto>>.Success(page);
    }

    public async Task<Result<int>> MarkReadAsync(long notificationId, CancellationToken cancellationToken)
    {
        if (baseService.UserId is not { } userId) return Result<int>.Unauthorized();
        var notification = await db.Notifications.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.RecipientUserId == userId, cancellationToken);
        if (notification is null) return Result<int>.NotFound("Notification not found.");
        if (notification.ReadAt is null) { notification.ReadAt = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken); }
        var remaining = await db.Notifications.CountAsync(
            x => x.RecipientUserId == userId && x.ReadAt == null, cancellationToken);
        return Result<int>.Success(remaining);
    }

    public async Task<Result<int>> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        if (baseService.UserId is not { } userId) return Result<int>.Unauthorized();
        var items = await db.Notifications.Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var item in items) item.ReadAt = now;
        if (items.Count > 0) await db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(0, $"Marked {items.Count} notifications as read.");
    }

    public static NotificationDto Map(Notification x) => new()
    {
        Id = x.Id, BorrowRecordId = x.BorrowRecordId, Type = x.Type, Title = x.Title,
        Message = x.Message, CreatedAt = x.CreatedAt, ReadAt = x.ReadAt
    };
}
