namespace Database.AppDbContextModels;

public class Book : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public long AuthorId { get; set; }

    public Author Author { get; set; } = default!;

    public long CategoryId { get; set; }

    public Category Category { get; set; } = default!;

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }
}