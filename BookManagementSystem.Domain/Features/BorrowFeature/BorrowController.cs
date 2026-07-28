using BookManagementSystem.Domain.Authorization;
using Contracts.Borrow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.BorrowFeature;

public sealed class BorrowController(
    IBorrowService borrowService,
    IBaseService baseService) : BaseController
{
    [Authorize(Policy = PermissionProvider.LibraryMemberOnly)]
    [HttpPost]
    public async Task<IActionResult> Borrow(
        [FromBody] BorrowBookRequest request,
        CancellationToken cancellationToken)
    {
        if (!baseService.UserId.HasValue)
        {
            return Unauthorized();
        }

        return Execute(
            await borrowService.BorrowBookAsync(
                baseService.UserId.Value,
                request,
                cancellationToken));
    }

    [Authorize(Policy = PermissionProvider.LibraryMemberOnly)]
    [HttpPut("{borrowRecordId:long}/return")]
    public async Task<IActionResult> Return(
        long borrowRecordId,
        CancellationToken cancellationToken)
    {
        if (!baseService.UserId.HasValue)
        {
            return Unauthorized();
        }

        return Execute(
            await borrowService.ReturnBookAsync(
                baseService.UserId.Value,
                borrowRecordId,
                cancellationToken));
    }

    [Authorize(Policy = PermissionProvider.LibraryMemberOnly)]
    [HttpGet("my-books")]
    public async Task<IActionResult> GetMyBorrowedBooks(
        CancellationToken cancellationToken)
    {
        if (!baseService.UserId.HasValue)
        {
            return Unauthorized();
        }

        return Execute(
            await borrowService.GetMyBorrowedBooksAsync(
                baseService.UserId.Value,
                cancellationToken));
    }

    [Authorize(Policy = PermissionProvider.LibraryMemberOnly)]
    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyHistory(
        CancellationToken cancellationToken)
    {
        if (!baseService.UserId.HasValue)
        {
            return Unauthorized();
        }

        return Execute(
            await borrowService.GetMyHistoryAsync(
                baseService.UserId.Value,
                cancellationToken));
    }

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpPost("history")]
    public async Task<IActionResult> GetBorrowHistory(
        [FromBody] BorrowFilterRequest request,
        CancellationToken cancellationToken)
    {
        return Execute(
            await borrowService.GetBorrowHistoryAsync(
                request,
                cancellationToken));
    }
}