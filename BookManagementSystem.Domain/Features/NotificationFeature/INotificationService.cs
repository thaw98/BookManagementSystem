using Contracts.Notification;
using Database.AppDbContextModels;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.NotificationFeature;

public interface INotificationService
{
    Task<List<Notification>> AddLibrarianNotificationsAsync(BookBorrowRecord record, string type,
        string title, string message, CancellationToken cancellationToken);
    Task DispatchAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken);
    Task<Result<NotificationInboxDto>> GetUnreadAsync(CancellationToken cancellationToken);
    Task<Result<int>> MarkReadAsync(long notificationId, CancellationToken cancellationToken);
    Task<Result<int>> MarkAllReadAsync(CancellationToken cancellationToken);
}
