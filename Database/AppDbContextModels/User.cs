namespace Database.AppDbContextModels;

public sealed class User : AuditableEntity
{
    public long RoleId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public Role Role { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<BookBorrowRecord> BookBorrowRecords { get; set; }
    = new List<BookBorrowRecord>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
