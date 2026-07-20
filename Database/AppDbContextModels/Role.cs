namespace Database.AppDbContextModels;

public sealed class Role : AuditableEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
