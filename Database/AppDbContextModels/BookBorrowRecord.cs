namespace Database.AppDbContextModels;

public class BookBorrowRecord : AuditableEntity
{
    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public long BookId { get; set; }

    public Book Book { get; set; } = null!;

    public DateTime BorrowedAt { get; set; }

    public DateTime DueAt { get; set; }

    public DateTime? ReturnedAt { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
