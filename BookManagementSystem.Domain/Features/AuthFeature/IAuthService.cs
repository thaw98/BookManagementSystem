using Contracts;
using Contracts.Auth;

namespace BookManagementSystem.Domain.Features.AuthFeature;

public interface IAuthService
{
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<TokenResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
}
