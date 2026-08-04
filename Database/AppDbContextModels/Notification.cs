namespace Database.AppDbContextModels;

public sealed class Notification : BaseEntity
{
    public long RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;
    public long BorrowRecordId { get; set; }
    public BookBorrowRecord BorrowRecord { get; set; } = null!;
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime? ReadAt { get; set; }
}
