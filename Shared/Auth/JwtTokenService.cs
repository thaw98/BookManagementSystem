using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes.");

        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(long userId, string email, string roleName)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, DateTime.UtcNow, expiresAt, _credentials);
        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var raw = Base64Url(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenResult(raw, HashRefreshToken(raw), DateTime.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
