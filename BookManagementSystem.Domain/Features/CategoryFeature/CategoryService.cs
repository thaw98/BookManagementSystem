using Contracts;
using Contracts.Category;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;

namespace BookManagementSystem.Domain.Features.CategoryFeature;

public sealed class CategoryService(AppDbContext db) : ICategoryService
{
    public async Task<Result<List<CategoryDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);

        return Result<List<CategoryDto>>.Success(categories);
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return category is null
            ? Result<CategoryDto>.NotFound("Category not found.")
            : Result<CategoryDto>.Success(category);
    }

    public async Task<Result<long>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);

        if (string.IsNullOrWhiteSpace(name))
            return Result<long>.Validation(
                "Category name is required.");

        var categoryExists = await db.Categories
            .AnyAsync(x => x.Name == name, cancellationToken);

        if (categoryExists)
            return Result<long>.Duplicate(
                "Category already exists.");

        var category = new Category
        {
            Name = name
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(category.Id);
    }

    public async Task<Result<CategoryDto>> UpdateAsync(
        long id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (category is null)
            return Result<CategoryDto>.NotFound(
                "Category not found.");

        var name = NormalizeName(request.Name);

        if (string.IsNullOrWhiteSpace(name))
            return Result<CategoryDto>.Validation(
                "Category name is required.");

        var duplicateExists = await db.Categories
            .AnyAsync(
                x => x.Id != id && x.Name == name,
                cancellationToken);

        if (duplicateExists)
            return Result<CategoryDto>.Duplicate(
                "Category already exists.");

        category.Name = name;

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (category is null)
            return Result<bool>.NotFound(
                "Category not found.");

        var hasBooks = await db.Books
            .AnyAsync(
                x => x.CategoryId == id,
                cancellationToken);

        if (hasBooks)
        {
            return Result<bool>.Validation(
                "This category cannot be deleted because books are using this category.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static string NormalizeName(string name) =>
        name.Trim();
}