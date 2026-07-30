using Contracts.Member;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;
using Shared.Constants;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.MemberFeature;

public sealed class MemberService(
    AppDbContext db,
    IPasswordHasher passwordHasher) : IMemberService
{
    public async Task<Result<List<MemberListDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var members = await db.Users
            .AsNoTracking()
            .Where(x => x.Role.Name == RoleNames.LibraryMember)
            .OrderBy(x => x.FullName)
            .Select(x => new MemberListDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<MemberListDto>>.Success(members);
    }

    public async Task<Result<OffsetPagedResult<MemberListDto>>> GetPagedAsync(
        MemberFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Users
            .AsNoTracking()
            .Where(x => x.Role.Name == RoleNames.LibraryMember)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();

            query = query.Where(
                x => x.FullName.Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();

            query = query.Where(
                x => x.Email.Contains(email));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(
                x => x.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(
                x =>
                    x.FullName.Contains(search) ||
                    x.Email.Contains(search));
        }

        var page = await Pagination.OffsetPagination.CreateAsync(
            query,
            request,
            source => ApplyPagedOrdering(source, request),
            x => new MemberListDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            },
            cancellationToken);

        return Result<OffsetPagedResult<MemberListDto>>
            .Success(page);
    }

    public async Task<Result<MemberDetailDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var member = await db.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == id &&
                x.Role.Name == RoleNames.LibraryMember)
            .Select(x => new MemberDetailDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (member is null)
        {
            return Result<MemberDetailDto>.NotFound(
                "Library member not found.");
        }

        return Result<MemberDetailDto>.Success(member);
    }

    public async Task<Result<long>> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<long>.Validation(
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<long>.Validation(
                "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<long>.Validation(
                "Password is required.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Result<long>.Validation(
                "Password and confirm password do not match.");
        }

        var duplicateEmail = await db.Users
            .AnyAsync(
                x => x.Email == email,
                cancellationToken);

        if (duplicateEmail)
        {
            return Result<long>.Duplicate(
                "This email is already registered.");
        }

        var memberRole = await db.Roles
            .FirstOrDefaultAsync(
                x => x.Name == RoleNames.LibraryMember,
                cancellationToken);

        if (memberRole is null)
        {
            return Result<long>.NotFound(
                "LibraryMember role was not found.");
        }

        var member = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash =
                passwordHasher.HashPassword(request.Password),
            RoleId = memberRole.Id,
            IsActive = request.IsActive
        };

        db.Users.Add(member);

        await db.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(member.Id);
    }

    public async Task<Result<MemberDetailDto>> UpdateAsync(
        long id,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        var member = await db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.Role.Name == RoleNames.LibraryMember,
                cancellationToken);

        if (member is null)
        {
            return Result<MemberDetailDto>.NotFound(
                "Library member not found.");
        }

        var fullName = request.FullName.Trim();
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<MemberDetailDto>.Validation(
                "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<MemberDetailDto>.Validation(
                "Email is required.");
        }

        var duplicateEmail = await db.Users
            .AnyAsync(
                x =>
                    x.Id != id &&
                    x.Email == email,
                cancellationToken);

        if (duplicateEmail)
        {
            return Result<MemberDetailDto>.Duplicate(
                "This email is already registered.");
        }

        member.FullName = fullName;
        member.Email = email;
        member.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(
            id,
            cancellationToken);
    }

    public async Task<Result<bool>> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var member = await db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.Role.Name == RoleNames.LibraryMember,
                cancellationToken);

        if (member is null)
        {
            return Result<bool>.NotFound(
                "Library member not found.");
        }

        db.Users.Remove(member);

        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static IOrderedQueryable<User> ApplyPagedOrdering(
        IQueryable<User> query,
        MemberFilterRequest request) =>
        (request.SortBy, request.SortDescending) switch
        {
            ("fullName", true) =>
                query
                    .OrderByDescending(x => x.FullName)
                    .ThenBy(x => x.Id),

            ("fullName", false) =>
                query
                    .OrderBy(x => x.FullName)
                    .ThenBy(x => x.Id),

            ("email", true) =>
                query
                    .OrderByDescending(x => x.Email)
                    .ThenBy(x => x.Id),

            ("email", false) =>
                query
                    .OrderBy(x => x.Email)
                    .ThenBy(x => x.Id),

            ("isActive", true) =>
                query
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.FullName)
                    .ThenBy(x => x.Id),

            ("isActive", false) =>
                query
                    .OrderBy(x => x.IsActive)
                    .ThenBy(x => x.FullName)
                    .ThenBy(x => x.Id),

            ("createdAt", true) =>
                query
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenBy(x => x.Id),

            ("createdAt", false) =>
                query
                    .OrderBy(x => x.CreatedAt)
                    .ThenBy(x => x.Id),

            _ =>
                query
                    .OrderBy(x => x.FullName)
                    .ThenBy(x => x.Id)
        };
}