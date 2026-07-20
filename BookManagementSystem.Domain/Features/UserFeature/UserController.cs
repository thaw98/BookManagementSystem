using BookManagementSystem.Domain.Authorization;
using Contracts.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.UserFeature;

[Authorize(Policy = PermissionProvider.AdminOnly)]
public sealed class UserController(IUserService userService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Execute(await userService.GetAllAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        Execute(await userService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken) =>
        Execute(await userService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        Execute(await userService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:long}/ResetPassword")]
    public async Task<IActionResult> ResetPassword(long id, ResetPasswordRequest request, CancellationToken cancellationToken) =>
        Execute(await userService.ResetPasswordAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        Execute(await userService.DeleteAsync(id, cancellationToken));
}
