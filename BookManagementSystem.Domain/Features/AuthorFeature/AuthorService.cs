using Contracts;
using Contracts.Author;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace BookManagementSystem.Domain.Features.AuthorFeature;

public sealed class AuthorService(AppDbContext db) : IAuthorService
{
    public async Task<Result<List<AuthorDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var authors = await db.Authors
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AuthorDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);

        return Result<List<AuthorDto>>.Success(authors);
    }

    public async Task<Result<AuthorDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AuthorDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return author is null
            ? Result<AuthorDto>.NotFound("Author not found.")
            : Result<AuthorDto>.Success(author);
    }

    public async Task<Result<long>> CreateAsync(
        CreateAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);

        if (string.IsNullOrWhiteSpace(name))
            return Result<long>.Validation("Author name is required.");

        var authorExists = await db.Authors
            .AnyAsync(x => x.Name == name, cancellationToken);

        if (authorExists)
            return Result<long>.Duplicate("Author already exists.");

        var author = new Author
        {
            Name = name
        };

        db.Authors.Add(author);
        await db.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(author.Id);
    }

    public async Task<Result<AuthorDto>> UpdateAsync(
        long id,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (author is null)
            return Result<AuthorDto>.NotFound("Author not found.");

        var name = NormalizeName(request.Name);

        if (string.IsNullOrWhiteSpace(name))
            return Result<AuthorDto>.Validation(
                "Author name is required.");

        var duplicateExists = await db.Authors
            .AnyAsync(
                x => x.Id != id && x.Name == name,
                cancellationToken);

        if (duplicateExists)
            return Result<AuthorDto>.Duplicate(
                "Author already exists.");

        author.Name = name;

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var author = await db.Authors
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (author is null)
            return Result<bool>.NotFound("Author not found.");

        var hasBooks = await db.Books
            .AnyAsync(x => x.AuthorId == id, cancellationToken);

        if (hasBooks)
        {
            return Result<bool>.Validation(
                "This author cannot be deleted because books are using this author.");
        }

        db.Authors.Remove(author);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static string NormalizeName(string name) =>
        name.Trim();
}