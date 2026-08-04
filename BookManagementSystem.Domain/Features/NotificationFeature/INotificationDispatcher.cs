using Contracts.Notification;

namespace BookManagementSystem.Domain.Features.NotificationFeature;

public interface INotificationDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<(long RecipientUserId, NotificationDto Notification)> notifications,
        CancellationToken cancellationToken = default);
}

public sealed class NullNotificationDispatcher : INotificationDispatcher
{
    public Task DispatchAsync(IReadOnlyCollection<(long RecipientUserId, NotificationDto Notification)> notifications,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
