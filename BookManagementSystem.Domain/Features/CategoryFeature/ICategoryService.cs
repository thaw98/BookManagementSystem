using Contracts.Category;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.CategoryFeature;

public interface ICategoryService
{
    Task<Result<List<CategoryDto>>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Result<OffsetPagedResult<CategoryDto>>> GetPagedAsync(
        CategoryFilterRequest request,
        CancellationToken cancellationToken);

    Task<Result<CategoryDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<Result<long>> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<Result<CategoryDto>> UpdateAsync(
        long id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken);

    Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken);
}
