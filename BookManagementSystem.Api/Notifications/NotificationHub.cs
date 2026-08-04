using System.Security.Claims;
using BookManagementSystem.Domain.Features.NotificationFeature;
using Contracts.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookManagementSystem.Api.Notifications;

[Authorize]
public sealed class NotificationHub : Hub;

public sealed class NotificationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

public sealed class SignalRNotificationDispatcher(
    IHubContext<NotificationHub> hub,
    ILogger<SignalRNotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<(long RecipientUserId, NotificationDto Notification)> notifications,
        CancellationToken cancellationToken = default)
    {
        foreach (var (recipientUserId, notification) in notifications)
        {
            try
            {
                await hub.Clients.User(recipientUserId.ToString())
                    .SendAsync("NotificationReceived", notification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not deliver notification {NotificationId} to user {UserId}.",
                    notification.Id, recipientUserId);
            }
        }
    }
}
