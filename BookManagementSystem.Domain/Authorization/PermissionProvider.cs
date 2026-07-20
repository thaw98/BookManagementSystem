using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shared.Constants;

namespace BookManagementSystem.Domain.Authorization;

public static class PermissionProvider
{
    public const string AdminOnly = nameof(AdminOnly);

    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireAuthenticatedUser().RequireRole(RoleNames.Admin));
        });

        return services;
    }
}
