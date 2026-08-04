using Contracts.Borrow;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.Models;
using BookManagementSystem.Domain.Features.NotificationFeature;

namespace BookManagementSystem.Domain.Features.BorrowFeature;

public sealed class BorrowService(
    AppDbContext db,
    INotificationService notificationService) : IBorrowService
{
    private const int MaxBorrowBooks = 5;
    private const int BorrowDays = 2;

    public async Task<Result<BorrowResultDto>> BorrowBookAsync(
        long userId,
        BorrowBookRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BookId <= 0)
        {
            return Result<BorrowResultDto>.Validation(
                "Please select a valid book.");
        }

        var member = await db.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == userId &&
                    x.IsActive &&
                    x.Role.Name == RoleNames.LibraryMember,
                cancellationToken);

        if (member is null)
        {
            return Result<BorrowResultDto>.NotFound(
                "Active library member not found.");
        }

        var book = await db.Books
            .FirstOrDefaultAsync(
                x => x.Id == request.BookId,
                cancellationToken);

        if (book is null)
        {
            return Result<BorrowResultDto>.NotFound(
                "Book not found.");
        }

        if (book.AvailableCopies <= 0)
        {
            return Result<BorrowResultDto>.Validation(
                "This book is currently unavailable.");
        }

        var alreadyBorrowed = await db.BookBorrowRecords
            .AnyAsync(
                x =>
                    x.UserId == userId &&
                    x.BookId == request.BookId &&
                    x.ReturnedAt == null,
                cancellationToken);

        if (alreadyBorrowed)
        {
            return Result<BorrowResultDto>.Validation(
                "You have already borrowed this book.");
        }

        var activeBorrowCount = await db.BookBorrowRecords
            .CountAsync(
                x =>
                    x.UserId == userId &&
                    x.ReturnedAt == null,
                cancellationToken);

        if (activeBorrowCount >= MaxBorrowBooks)
        {
            return Result<BorrowResultDto>.Validation(
                $"A member can borrow up to {MaxBorrowBooks} books.");
        }

        var now = DateTime.UtcNow;

        var borrowRecord = new BookBorrowRecord
        {
            UserId = userId,
            BookId = book.Id,
            BorrowedAt = now,
            DueAt = now.AddDays(BorrowDays)
        };

        db.BookBorrowRecords.Add(borrowRecord);

        book.AvailableCopies--;

        var borrowNotifications = await notificationService.AddLibrarianNotificationsAsync(
            borrowRecord, "Borrowed", "Book borrowed",
            $"{member.FullName} borrowed \"{book.Title}\" at {now:O}. Due at {borrowRecord.DueAt:O}.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await notificationService.DispatchAsync(borrowNotifications, cancellationToken);

        var result = new BorrowResultDto
        {
            BorrowRecordId = borrowRecord.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            BorrowedAt = borrowRecord.BorrowedAt,
            DueAt = borrowRecord.DueAt,
            AvailableCopies = book.AvailableCopies
        };

        return Result<BorrowResultDto>.Success(
            result,
            "Book borrowed successfully.");
    }

    public async Task<Result<ReturnResultDto>> ReturnBookAsync(
        long userId,
        long borrowRecordId,
        CancellationToken cancellationToken)
    {
        if (borrowRecordId <= 0)
        {
            return Result<ReturnResultDto>.Validation(
                "Invalid borrow record.");
        }

        var borrowRecord = await db.BookBorrowRecords
            .Include(x => x.Book)
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == borrowRecordId &&
                    x.UserId == userId,
                cancellationToken);

        if (borrowRecord is null)
        {
            return Result<ReturnResultDto>.NotFound(
                "Borrow record not found.");
        }

        if (borrowRecord.ReturnedAt.HasValue)
        {
            return Result<ReturnResultDto>.Validation(
                "This book has already been returned.");
        }

        var returnedAt = DateTime.UtcNow;

        borrowRecord.ReturnedAt = returnedAt;

        if (borrowRecord.Book.AvailableCopies <
            borrowRecord.Book.TotalCopies)
        {
            borrowRecord.Book.AvailableCopies++;
        }

        var returnNotifications = await notificationService.AddLibrarianNotificationsAsync(
            borrowRecord, "Returned", "Book returned",
            $"{borrowRecord.User.FullName} returned \"{borrowRecord.Book.Title}\" at {returnedAt:O}. " +
            $"Available copies: {borrowRecord.Book.AvailableCopies} of {borrowRecord.Book.TotalCopies}.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await notificationService.DispatchAsync(returnNotifications, cancellationToken);

        var result = new ReturnResultDto
        {
            BorrowRecordId = borrowRecord.Id,
            BookId = borrowRecord.BookId,
            BookTitle = borrowRecord.Book.Title,
            ReturnedAt = returnedAt,
            AvailableCopies =
                borrowRecord.Book.AvailableCopies
        };

        return Result<ReturnResultDto>.Success(
            result,
            "Book returned successfully.");
    }

    public async Task<Result<List<ActiveBorrowDto>>>
        GetMyBorrowedBooksAsync(
            long userId,
            CancellationToken cancellationToken)
    {
        var records = await db.BookBorrowRecords
            .AsNoTracking()
            .Where(
                x =>
                    x.UserId == userId &&
                    x.ReturnedAt == null)
            .OrderByDescending(x => x.BorrowedAt)
            .Select(x => new
            {
                x.Id,
                x.BookId,
                BookTitle = x.Book.Title,
                AuthorName = x.Book.Author.Name,
                CategoryName = x.Book.Category.Name,
                x.BorrowedAt,
                x.DueAt,
                x.ReturnedAt
            })
            .ToListAsync(cancellationToken);

        var result = records
            .Select(x => new ActiveBorrowDto
            {
                Id = x.Id,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                AuthorName = x.AuthorName,
                CategoryName = x.CategoryName,
                BorrowedAt = x.BorrowedAt,
                DueAt = x.DueAt,
                RemainingDays = GetRemainingDays(
                    x.DueAt,
                    x.ReturnedAt),
                Status = GetStatus(
                    x.DueAt,
                    x.ReturnedAt)
            })
            .ToList();

        return Result<List<ActiveBorrowDto>>.Success(
            result);
    }

    public async Task<Result<List<BorrowHistoryDto>>>
        GetMyHistoryAsync(
            long userId,
            CancellationToken cancellationToken)
    {
        var records = await db.BookBorrowRecords
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.BorrowedAt)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                MemberName = x.User.FullName,
                MemberEmail = x.User.Email,
                x.BookId,
                BookTitle = x.Book.Title,
                AuthorName = x.Book.Author.Name,
                CategoryName = x.Book.Category.Name,
                x.BorrowedAt,
                x.DueAt,
                x.ReturnedAt
            })
            .ToListAsync(cancellationToken);

        var result = records
            .Select(x => new BorrowHistoryDto
            {
                Id = x.Id,
                UserId = x.UserId,
                MemberName = x.MemberName,
                MemberEmail = x.MemberEmail,
                BookId = x.BookId,
                BookTitle = x.BookTitle,
                AuthorName = x.AuthorName,
                CategoryName = x.CategoryName,
                BorrowedAt = x.BorrowedAt,
                DueAt = x.DueAt,
                ReturnedAt = x.ReturnedAt,
                RemainingDays = GetRemainingDays(
                    x.DueAt,
                    x.ReturnedAt),
                Status = GetStatus(
                    x.DueAt,
                    x.ReturnedAt)
            })
            .ToList();

        return Result<List<BorrowHistoryDto>>.Success(
            result);
    }

    public async Task<
        Result<OffsetPagedResult<BorrowHistoryDto>>>
        GetBorrowHistoryAsync(
            BorrowFilterRequest request,
            CancellationToken cancellationToken)
    {
        var query = db.BookBorrowRecords
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(
                x =>
                    x.User.FullName.Contains(search) ||
                    x.User.Email.Contains(search) ||
                    x.Book.Title.Contains(search) ||
                    x.Book.Author.Name.Contains(search) ||
                    x.Book.Category.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.MemberName))
        {
            var memberName = request.MemberName.Trim();

            query = query.Where(
                x => x.User.FullName.Contains(memberName));
        }

        if (!string.IsNullOrWhiteSpace(request.BookTitle))
        {
            var bookTitle = request.BookTitle.Trim();

            query = query.Where(
                x => x.Book.Title.Contains(bookTitle));
        }

        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var author = request.Author.Trim();

            query = query.Where(
                x => x.Book.Author.Name.Contains(author));
        }

        if (request.CategoryId.HasValue &&
            request.CategoryId.Value > 0)
        {
            query = query.Where(
                x =>
                    x.Book.CategoryId ==
                    request.CategoryId.Value);
        }

        if (request.BorrowedFrom.HasValue)
        {
            var borrowedFrom =
                request.BorrowedFrom.Value.Date;

            query = query.Where(
                x => x.BorrowedAt >= borrowedFrom);
        }

        if (request.BorrowedTo.HasValue)
        {
            var borrowedToExclusive =
                request.BorrowedTo.Value
                    .Date
                    .AddDays(1);

            query = query.Where(
                x =>
                    x.BorrowedAt <
                    borrowedToExclusive);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status
                .Trim()
                .ToLowerInvariant();

            var now = DateTime.UtcNow;

            switch (status)
            {
                case "borrowed":
                    query = query.Where(
                        x =>
                            x.ReturnedAt == null &&
                            x.DueAt >= now);
                    break;

                case "overdue":
                    query = query.Where(
                        x =>
                            x.ReturnedAt == null &&
                            x.DueAt < now);
                    break;

                case "returned":
                    query = query.Where(
                        x => x.ReturnedAt != null);
                    break;

                default:
                    return Result<
                        OffsetPagedResult<BorrowHistoryDto>>
                        .Validation(
                            "Status must be Borrowed, Overdue, or Returned.");
            }
        }

        var page = await Pagination.OffsetPagination.CreateAsync(
            query,
            request,
            source => ApplyBorrowHistoryOrdering(
                source,
                request),
            x => new BorrowHistoryDto
            {
                Id = x.Id,
                UserId = x.UserId,
                MemberName = x.User.FullName,
                MemberEmail = x.User.Email,
                BookId = x.BookId,
                BookTitle = x.Book.Title,
                AuthorName = x.Book.Author.Name,
                CategoryName = x.Book.Category.Name,
                BorrowedAt = x.BorrowedAt,
                DueAt = x.DueAt,
                ReturnedAt = x.ReturnedAt
            },
            cancellationToken);

        foreach (var item in page.Items)
        {
            item.RemainingDays = GetRemainingDays(
                item.DueAt,
                item.ReturnedAt);

            item.Status = GetStatus(
                item.DueAt,
                item.ReturnedAt);
        }

        return Result<
            OffsetPagedResult<BorrowHistoryDto>>
            .Success(page);
    }

    private static IOrderedQueryable<BookBorrowRecord>
        ApplyBorrowHistoryOrdering(
            IQueryable<BookBorrowRecord> query,
            BorrowFilterRequest request) =>
        (request.SortBy, request.SortDescending) switch
        {
            ("memberName", true) =>
                query
                    .OrderByDescending(
                        x => x.User.FullName)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("memberName", false) =>
                query
                    .OrderBy(
                        x => x.User.FullName)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("bookTitle", true) =>
                query
                    .OrderByDescending(
                        x => x.Book.Title)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("bookTitle", false) =>
                query
                    .OrderBy(
                        x => x.Book.Title)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("author", true) =>
                query
                    .OrderByDescending(
                        x => x.Book.Author.Name)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("author", false) =>
                query
                    .OrderBy(
                        x => x.Book.Author.Name)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("category", true) =>
                query
                    .OrderByDescending(
                        x => x.Book.Category.Name)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("category", false) =>
                query
                    .OrderBy(
                        x => x.Book.Category.Name)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("borrowedAt", true) =>
                query
                    .OrderByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("borrowedAt", false) =>
                query
                    .OrderBy(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("dueAt", true) =>
                query
                    .OrderByDescending(
                        x => x.DueAt)
                    .ThenBy(x => x.Id),

            ("dueAt", false) =>
                query
                    .OrderBy(
                        x => x.DueAt)
                    .ThenBy(x => x.Id),

            ("returnedAt", true) =>
                query
                    .OrderByDescending(
                        x => x.ReturnedAt)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            ("returnedAt", false) =>
                query
                    .OrderBy(
                        x => x.ReturnedAt)
                    .ThenByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id),

            _ =>
                query
                    .OrderByDescending(
                        x => x.BorrowedAt)
                    .ThenBy(x => x.Id)
        };

    private static string GetStatus(
        DateTime dueAt,
        DateTime? returnedAt)
    {
        if (returnedAt.HasValue)
        {
            return "Returned";
        }

        return dueAt < DateTime.UtcNow
            ? "Overdue"
            : "Borrowed";
    }

    private static int GetRemainingDays(
        DateTime dueAt,
        DateTime? returnedAt)
    {
        if (returnedAt.HasValue ||
            dueAt < DateTime.UtcNow)
        {
            return 0;
        }

        return Math.Max(
            0,
            (dueAt.Date - DateTime.UtcNow.Date).Days);
    }
}
