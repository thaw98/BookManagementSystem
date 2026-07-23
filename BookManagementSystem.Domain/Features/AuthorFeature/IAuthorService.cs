using Contracts;
using Contracts.Author;

namespace BookManagementSystem.Domain.Features.AuthorFeature;

public interface IAuthorService
{
    Task<Result<List<AuthorDto>>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Result<AuthorDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<Result<long>> CreateAsync(
        CreateAuthorRequest request,
        CancellationToken cancellationToken);

    Task<Result<AuthorDto>> UpdateAsync(
        long id,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken);

    Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken);
}