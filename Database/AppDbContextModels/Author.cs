namespace Database.AppDbContextModels;

public class Author : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; }
        = new List<Book>();
}