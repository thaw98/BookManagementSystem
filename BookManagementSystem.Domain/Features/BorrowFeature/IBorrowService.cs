using Contracts;
using Contracts.Borrow;

namespace BookManagementSystem.Domain.Features.BorrowFeature;

public interface IBorrowService
{
    Task<Result<BorrowResultDto>> BorrowBookAsync(
        long userId,
        BorrowBookRequest request,
        CancellationToken cancellationToken);

    Task<Result<ReturnResultDto>> ReturnBookAsync(
        long userId,
        long borrowRecordId,
        CancellationToken cancellationToken);

    Task<Result<List<ActiveBorrowDto>>> GetMyBorrowedBooksAsync(
        long userId,
        CancellationToken cancellationToken);

    Task<Result<List<BorrowHistoryDto>>> GetMyHistoryAsync(
        long userId,
        CancellationToken cancellationToken);

    Task<Result<List<BorrowHistoryDto>>> GetBorrowHistoryAsync(
        BorrowFilterRequest request,
        CancellationToken cancellationToken);
}