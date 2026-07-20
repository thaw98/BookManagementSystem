namespace Shared.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "BookManagementSystem";
    public string Audience { get; set; } = "BookManagementSystem";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
