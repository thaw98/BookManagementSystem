using Contracts;
using Contracts.Auth;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;

namespace BookManagementSystem.Domain.Features.AuthFeature;

public sealed class AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<TokenResponse>.Unauthorized();

        var user = await db.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
            return Result<TokenResponse>.Unauthorized();

        return await IssueTokenPairAsync(user, null, cancellationToken);
    }

    public async Task<Result<TokenResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<TokenResponse>.Unauthorized();

        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        var token = await db.RefreshTokens.Include(x => x.User).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is null || token.RevokedAt.HasValue || token.ExpiresAt <= DateTime.UtcNow || token.User.IsDeleted || !token.User.IsActive)
            return Result<TokenResponse>.Unauthorized();

        return await IssueTokenPairAsync(token.User, token, cancellationToken);
    }

    public async Task<Result<bool>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);
            var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
            if (token is not null && !token.RevokedAt.HasValue)
            {
                token.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<TokenResponse>> IssueTokenPairAsync(User user, RefreshToken? tokenToRotate, CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenService.CreateAccessToken(user.Id, user.Email, user.Role.Name);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        if (tokenToRotate is not null)
        {
            tokenToRotate.RevokedAt = DateTime.UtcNow;
            tokenToRotate.ReplacedByTokenHash = refreshToken.TokenHash;
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshToken.ExpiresAt
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result<TokenResponse>.Success(new TokenResponse
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.RawToken,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.Name
        });
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
