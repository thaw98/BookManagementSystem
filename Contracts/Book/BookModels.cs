namespace Contracts.Book;

public class BookListDto
{
    public long Id { get; set; }

    public string Title { get; set; } = "";

    public long AuthorId { get; set; }

    public string AuthorName { get; set; } = "";

    public long CategoryId { get; set; }

    public string CategoryName { get; set; } = "";

    public int TotalCopies { get; set; }

    public int AvailableCopies { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class BookDetailDto : BookListDto
{
    public DateTime UpdatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public long? UpdatedBy { get; set; }
}

public sealed class CreateBookRequest
{
    public string Title { get; set; } = "";

    public long AuthorId { get; set; }

    public long CategoryId { get; set; }

    public int TotalCopies { get; set; }
}

public sealed class UpdateBookRequest
{
    public string Title { get; set; } = "";

    public long AuthorId { get; set; }

    public long CategoryId { get; set; }

    public int TotalCopies { get; set; }
}

public sealed class BookFilterRequest
{
    public string? Title { get; set; }

    public string? Author { get; set; }

    public long? CategoryId { get; set; }
}