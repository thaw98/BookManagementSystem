namespace Database.AppDbContextModels;

public sealed class User : AuditableEntity
{
    public long RoleId { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public Role Role { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
