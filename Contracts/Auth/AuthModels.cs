namespace Contracts.Auth;

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = "";
}

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = "";
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public long UserId { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}
