namespace Contracts.Book;

using System.ComponentModel.DataAnnotations;

public record class BookListDto
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

public record class BookDetailDto : BookListDto
{
    public DateTime UpdatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public long? UpdatedBy { get; set; }
}

public record class CreateBookRequest
{
    [Required]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = "";

    [Required]
    public long AuthorId { get; set; }

    [Required]
    public long CategoryId { get; set; }

    [Required]
    public int TotalCopies { get; set; }
}

public record class UpdateBookRequest
{
    [Required]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = "";

    [Required]
    public long AuthorId { get; set; }

    [Required]
    public long CategoryId { get; set; }

    [Required]
    public int TotalCopies { get; set; }
}

public record class BookFilterRequest : Shared.Models.OffsetPagedRequest
{
    public string? Title { get; set; }

    public string? Author { get; set; }

    public long? CategoryId { get; set; }
}
