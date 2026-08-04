using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.NotificationFeature;

public sealed class NotificationController(INotificationService service) : BaseController
{
    [HttpGet("unread")]
    public async Task<IActionResult> Unread(CancellationToken cancellationToken) =>
        Execute(await service.GetUnreadAsync(cancellationToken));

    [HttpPut("{notificationId:long}/read")]
    public async Task<IActionResult> Read(long notificationId, CancellationToken cancellationToken) =>
        Execute(await service.MarkReadAsync(notificationId, cancellationToken));

    [HttpPut("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken cancellationToken) =>
        Execute(await service.MarkAllReadAsync(cancellationToken));
}
