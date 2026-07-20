using Contracts;
using Contracts.User;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Base;
using Shared.Constants;

namespace BookManagementSystem.Domain.Features.UserFeature;

public sealed class UserService(AppDbContext db, IPasswordHasher passwordHasher, IBaseService baseService) : IUserService
{
    public async Task<Result<List<UserListDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(x => x.Email)
            .Select(x => new UserListDto
            {
                Id = x.Id,
                Email = x.Email,
                RoleId = x.RoleId,
                RoleName = x.Role.Name,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<UserListDto>>.Success(users);
    }

    public async Task<Result<UserDetailDto>> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDetailDto
            {
                Id = u.Id,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.Role.Name,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                CreatedBy = u.CreatedBy,
                UpdatedBy = u.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? Result<UserDetailDto>.NotFound() : Result<UserDetailDto>.Success(user);
    }

    public async Task<Result<long>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<long>.Validation("Email and password are required.");

        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == email, cancellationToken))
            return Result<long>.Duplicate("Email already exists.");

        if (!await db.Roles.AnyAsync(x => x.Id == request.RoleId, cancellationToken))
            return Result<long>.NotFound("Role not found.");

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            RoleId = request.RoleId,
            IsActive = request.IsActive
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(user.Id);
    }

    public async Task<Result<UserDetailDto>> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Result<UserDetailDto>.NotFound();

        var email = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email))
            return Result<UserDetailDto>.Validation("Email is required.");

        if (!await db.Roles.AnyAsync(x => x.Id == request.RoleId, cancellationToken))
            return Result<UserDetailDto>.NotFound("Role not found.");

        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id != id && x.Email == email, cancellationToken))
            return Result<UserDetailDto>.Duplicate("Email already exists.");

        var currentUserId = baseService.UserId;
        if (currentUserId == id && !request.IsActive)
            return Result<UserDetailDto>.Validation("You cannot deactivate your own account.");

        var removesAdmin = user.RoleId == RoleNames.AdminId && (request.RoleId != RoleNames.AdminId || !request.IsActive);
        if (removesAdmin && !await HasAnotherActiveAdminAsync(id, cancellationToken))
            return Result<UserDetailDto>.Validation("The system must keep at least one active Admin.");

        var securityChanged = user.Email != email || user.RoleId != request.RoleId || user.IsActive != request.IsActive;
        user.Email = email;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;

        if (securityChanged)
            await RevokeUserTokensAsync(id, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<bool>> ResetPasswordAsync(long id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return Result<bool>.Validation("Password is required.");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Result<bool>.NotFound();

        user.PasswordHash = passwordHasher.HashPassword(request.Password);
        await RevokeUserTokensAsync(id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Result<bool>.NotFound();

        if (baseService.UserId == id)
            return Result<bool>.Validation("You cannot delete your own account.");

        if (user.RoleId == RoleNames.AdminId && !await HasAnotherActiveAdminAsync(id, cancellationToken))
            return Result<bool>.Validation("The system must keep at least one active Admin.");

        await RevokeUserTokensAsync(id, cancellationToken);
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private async Task<bool> HasAnotherActiveAdminAsync(long id, CancellationToken cancellationToken) =>
        await db.Users.AnyAsync(x => x.Id != id && x.RoleId == RoleNames.AdminId && x.IsActive, cancellationToken);

    private async Task RevokeUserTokensAsync(long userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var tokens = await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
            token.RevokedAt = now;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
