using Contracts;
using Contracts.User;

namespace BookManagementSystem.Domain.Features.UserFeature;

public interface IUserService
{
    Task<Result<List<UserListDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Result<long>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> ResetPasswordAsync(long id, ResetPasswordRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(long id, CancellationToken cancellationToken);
}
