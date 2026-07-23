using BookManagementSystem.Domain.Features.AuthFeature;
using BookManagementSystem.Domain.Features.RoleFeature;
using BookManagementSystem.Domain.Features.UserFeature;
using BookManagementSystem.Domain.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Shared.Base;

namespace BookManagementSystem.Domain;

public static class FeatureManager
{
    public static IServiceCollection AddDomainFeatures(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IBaseService, BaseService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDbSeeder, DbSeeder>();
        return services;
    }
}
