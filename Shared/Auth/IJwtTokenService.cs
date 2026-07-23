namespace Shared.Auth;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
public sealed record RefreshTokenResult(string RawToken, string TokenHash, DateTime ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(long userId, string email, string roleName);
    RefreshTokenResult CreateRefreshToken();
    string HashRefreshToken(string rawToken);
}
