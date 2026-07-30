using Contracts.Member;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.MemberFeature;

public interface IMemberService
{
    Task<Result<List<MemberListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<OffsetPagedResult<MemberListDto>>> GetPagedAsync(
        MemberFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MemberDetailDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<Result<long>> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MemberDetailDto>> UpdateAsync(
        long id,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);
}