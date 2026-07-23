using Contracts;
using Contracts.Role;

namespace BookManagementSystem.Domain.Features.RoleFeature;

public interface IRoleService
{
    Task<Result<List<RoleListDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<RoleDetailDto>> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Result<long>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<Result<RoleDetailDto>> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(long id, CancellationToken cancellationToken);
}
