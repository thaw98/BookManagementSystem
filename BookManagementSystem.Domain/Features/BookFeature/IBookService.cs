using Contracts;
using Contracts.Book;

namespace BookManagementSystem.Domain.Features.BookFeature;

public interface IBookService
{
    Task<Result<List<BookListDto>>> GetAllAsync(
        BookFilterRequest filter,
        CancellationToken cancellationToken);

    Task<Result<BookDetailDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);

    Task<Result<long>> CreateAsync(
        CreateBookRequest request,
        CancellationToken cancellationToken);

    Task<Result<BookDetailDto>> UpdateAsync(
        long id,
        UpdateBookRequest request,
        CancellationToken cancellationToken);

    Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken);
}