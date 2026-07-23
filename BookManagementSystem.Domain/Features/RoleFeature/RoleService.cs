using Contracts;
using Contracts.Role;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace BookManagementSystem.Domain.Features.RoleFeature;

public sealed class RoleService(AppDbContext db) : IRoleService
{
    public async Task<Result<List<RoleListDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new RoleListDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsProtected = x.Id == RoleNames.AdminId || x.Id == RoleNames.LibrarianId || x.Id == RoleNames.LibraryMemberId
            })
            .ToListAsync(cancellationToken);

        return Result<List<RoleListDto>>.Success(roles);
    }

    public async Task<Result<RoleDetailDto>> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new RoleDetailDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsProtected = x.Id == RoleNames.AdminId || x.Id == RoleNames.LibrarianId || x.Id == RoleNames.LibraryMemberId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        return role is null ? Result<RoleDetailDto>.NotFound() : Result<RoleDetailDto>.Success(role);
    }

    public async Task<Result<long>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(name))
            return Result<long>.Validation("Role name is required.");

        if (await db.Roles.AnyAsync(x => x.Name == name, cancellationToken))
            return Result<long>.Duplicate("Role name already exists.");

        var role = new Role { Name = name, Description = CleanDescription(request.Description) };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(role.Id);
    }

    public async Task<Result<RoleDetailDto>> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
            return Result<RoleDetailDto>.NotFound();

        var name = NormalizeName(request.Name);
        if (string.IsNullOrWhiteSpace(name))
            return Result<RoleDetailDto>.Validation("Role name is required.");

        if (RoleNames.IsProtected(id) && name != role.Name)
            return Result<RoleDetailDto>.Validation("Built-in role names cannot be changed.");

        if (!RoleNames.IsProtected(id) && await db.Roles.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken))
            return Result<RoleDetailDto>.Duplicate("Role name already exists.");

        role.Name = RoleNames.IsProtected(id) ? role.Name : name;
        role.Description = CleanDescription(request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<Result<bool>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        if (RoleNames.IsProtected(id))
            return Result<bool>.Validation("Built-in roles cannot be deleted.");

        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
            return Result<bool>.NotFound();

        var hasUsers = await db.Users.AnyAsync(x => x.RoleId == id, cancellationToken);
        if (hasUsers)
            return Result<bool>.Validation("This role is assigned to one or more users and cannot be deleted.");

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static string NormalizeName(string name) => name.Trim();
    private static string? CleanDescription(string? description) => string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
