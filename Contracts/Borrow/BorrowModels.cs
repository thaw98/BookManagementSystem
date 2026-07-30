using Shared.Models;

namespace Contracts.Borrow;

public sealed class BorrowBookRequest
{
    public long BookId { get; set; }
}

public class ActiveBorrowDto
{
    public long Id { get; set; }

    public long BookId { get; set; }

    public string BookTitle { get; set; } = "";

    public string AuthorName { get; set; } = "";

    public string CategoryName { get; set; } = "";

    public DateTime BorrowedAt { get; set; }

    public DateTime DueAt { get; set; }

    public int RemainingDays { get; set; }

    public string Status { get; set; } = "";
}

public sealed class BorrowHistoryDto : ActiveBorrowDto
{
    public long UserId { get; set; }

    public string MemberName { get; set; } = "";

    public string MemberEmail { get; set; } = "";

    public DateTime? ReturnedAt { get; set; }
}

public record class BorrowFilterRequest
    : OffsetPagedRequest
{
    public string? MemberName { get; set; }

    public string? BookTitle { get; set; }

    public string? Author { get; set; }

    public long? CategoryId { get; set; }

    public string? Status { get; set; }

    public DateTime? BorrowedFrom { get; set; }

    public DateTime? BorrowedTo { get; set; }
}

public sealed class BorrowResultDto
{
    public long BorrowRecordId { get; set; }

    public long BookId { get; set; }

    public string BookTitle { get; set; } = "";

    public DateTime BorrowedAt { get; set; }

    public DateTime DueAt { get; set; }

    public int AvailableCopies { get; set; }
}

public sealed class ReturnResultDto
{
    public long BorrowRecordId { get; set; }

    public long BookId { get; set; }

    public string BookTitle { get; set; } = "";

    public DateTime ReturnedAt { get; set; }

    public int AvailableCopies { get; set; }
}