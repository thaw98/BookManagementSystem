namespace Contracts.Notification;

public sealed class NotificationDto
{
    public long Id { get; set; }
    public long BorrowRecordId { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public sealed class NotificationInboxDto
{
    public int UnreadCount { get; set; }
    public List<NotificationDto> Notifications { get; set; } = [];
}
