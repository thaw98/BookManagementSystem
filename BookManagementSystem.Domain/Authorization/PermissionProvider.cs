using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shared.Constants;

namespace BookManagementSystem.Domain.Authorization;

public static class PermissionProvider
{
    public const string AdminOnly = nameof(AdminOnly);

    public const string LibrarianOnly = nameof(LibrarianOnly);

    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Admin));

            options.AddPolicy(LibrarianOnly, policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole(RoleNames.Librarian));
        });

        return services;
    }
}
