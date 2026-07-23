using Contracts;
using Contracts.Book;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace BookManagementSystem.Domain.Features.BookFeature;

public sealed class BookService(AppDbContext db) : IBookService
{
    public async Task<Result<List<BookListDto>>> GetAllAsync(
        BookFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var query = db.Books
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var title = filter.Title.Trim();

            query = query.Where(x =>
                x.Title.Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(filter.Author))
        {
            var author = filter.Author.Trim();

            query = query.Where(x =>
                x.Author.Name.Contains(author));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(x =>
                x.CategoryId == filter.CategoryId.Value);
        }

        var books = await query
            .OrderBy(x => x.Title)
            .Select(x => new BookListDto
            {
                Id = x.Id,
                Title = x.Title,
                AuthorId = x.AuthorId,
                AuthorName = x.Author.Name,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                TotalCopies = x.TotalCopies,
                AvailableCopies = x.AvailableCopies,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<BookListDto>>.Success(books);
    }

    public async Task<Result<BookDetailDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var book = await db.Books
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BookDetailDto
            {
                Id = x.Id,
                Title = x.Title,
                AuthorId = x.AuthorId,
                AuthorName = x.Author.Name,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                TotalCopies = x.TotalCopies,
                AvailableCopies = x.AvailableCopies,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        return book is null
            ? Result<BookDetailDto>.NotFound("Book not found.")
            : Result<BookDetailDto>.Success(book);
    }

    public async Task<Result<long>> CreateAsync(
        CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var title = NormalizeTitle(request.Title);

        if (string.IsNullOrWhiteSpace(title))
            return Result<long>.Validation(
                "Book title is required.");

        if (request.TotalCopies <= 0)
            return Result<long>.Validation(
                "Total copies must be greater than zero.");

        var authorExists = await db.Authors
            .AnyAsync(
                x => x.Id == request.AuthorId,
                cancellationToken);

        if (!authorExists)
            return Result<long>.NotFound(
                "Author not found.");

        var categoryExists = await db.Categories
            .AnyAsync(
                x => x.Id == request.CategoryId,
                cancellationToken);

        if (!categoryExists)
            return Result<long>.NotFound(
                "Category not found.");

        var duplicateExists = await db.Books
            .AnyAsync(
                x => x.Title == title &&
                     x.AuthorId == request.AuthorId,
                cancellationToken);

        if (duplicateExists)
            return Result<long>.Duplicate(
                "This book already exists for the selected author.");

        var book = new Book
        {
            Title = title,
            AuthorId = request.AuthorId,
            CategoryId = request.CategoryId,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(book.Id);
    }

    public async Task<Result<BookDetailDto>> UpdateAsync(
        long id,
        UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var book = await db.Books
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (book is null)
            return Result<BookDetailDto>.NotFound(
                "Book not found.");

        var title = NormalizeTitle(request.Title);

        if (string.IsNullOrWhiteSpace(title))
            return Result<BookDetailDto>.Validation(
                "Book title is required.");

        if (request.TotalCopies <= 0)
            return Result<BookDetailDto>.Validation(
                "Total copies must be greater than zero.");

        var authorExists = await db.Authors
            .AnyAsync(
                x => x.Id == request.AuthorId,
                cancellationToken);

        if (!authorExists)
            return Result<BookDetailDto>.NotFound(
                "Author not found.");

        var categoryExists = await db.Categories
            .AnyAsync(
                x => x.Id == request.CategoryId,
                cancellationToken);

        if (!categoryExists)
            return Result<BookDetailDto>.NotFound(
                "Category not found.");

        var duplicateExists = await db.Books
            .AnyAsync(
                x => x.Id != id &&
                     x.Title == title &&
                     x.AuthorId == request.AuthorId,
                cancellationToken);

        if (duplicateExists)
            return Result<BookDetailDto>.Duplicate(
                "This book already exists for the selected author.");

        var borrowedCopies =
            book.TotalCopies - book.AvailableCopies;

        if (request.TotalCopies < borrowedCopies)
        {
            return Result<BookDetailDto>.Validation(
                $"Total copies cannot be less than the borrowed copy count ({borrowedCopies}).");
        }

        book.Title = title;
        book.AuthorId = request.AuthorId;
        book.CategoryId = request.CategoryId;
        book.TotalCopies = request.TotalCopies;
        book.AvailableCopies =
            request.TotalCopies - borrowedCopies;

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var book = await db.Books
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (book is null)
            return Result<bool>.NotFound(
                "Book not found.");

        var borrowedCopies =
            book.TotalCopies - book.AvailableCopies;

        if (borrowedCopies > 0)
        {
            return Result<bool>.Validation(
                "This book cannot be deleted while copies are borrowed.");
        }

        db.Books.Remove(book);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static string NormalizeTitle(string title) =>
        title.Trim();
}