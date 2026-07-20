using Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.AuthFeature;

[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("Login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Execute(await authService.LoginAsync(request, cancellationToken));

    [HttpPost("Refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        Execute(await authService.RefreshAsync(request, cancellationToken));

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken) =>
        Execute(await authService.LogoutAsync(request, cancellationToken));
}
