using System.Security.Claims;
using Contracts;
using Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApp.Services;

public sealed class AuthService(IHttpClientFactory httpClientFactory, TokenStorageService tokenStorage)
{
    public async Task<Result<bool>> SignInAsync(HttpContext httpContext, string email, string password, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("BmsApi");
        var response = await client.PostAsJsonAsync("api/Auth/Login", new LoginRequest { Email = email, Password = password }, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<Result<TokenResponse>>(cancellationToken: cancellationToken);
        if (result?.IsSuccess != true || result.Data is null)
            return Result<bool>.Unauthorized("Invalid email or password.");

        var sessionId = Guid.NewGuid().ToString("N");
        tokenStorage.Set(sessionId, new TokenEntry(result.Data.AccessToken, result.Data.RefreshToken));

        var claims = new List<Claim>
        {
            new("sid", sessionId),
            new(ClaimTypes.NameIdentifier, result.Data.UserId.ToString()),
            new(ClaimTypes.Email, result.Data.Email),
            new(ClaimTypes.Role, result.Data.Role)
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        return Result<bool>.Success(true);
    }

    public async Task SignOutAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var sessionId = httpContext.User.FindFirstValue("sid");
        if (!string.IsNullOrWhiteSpace(sessionId) && tokenStorage.Get(sessionId) is { } entry)
        {
            try
            {
                var client = httpClientFactory.CreateClient("BmsApi");
                await client.PostAsJsonAsync("api/Auth/Logout", new LogoutRequest { RefreshToken = entry.RefreshToken }, cancellationToken);
            }
            catch
            {
                // Local cleanup must happen even if the API is not reachable.
            }

            tokenStorage.Remove(sessionId);
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
