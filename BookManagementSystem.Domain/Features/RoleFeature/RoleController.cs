using BookManagementSystem.Domain.Authorization;
using Contracts.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.RoleFeature;

[Authorize(Policy = PermissionProvider.AdminOnly)]
public sealed class RoleController(IRoleService roleService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Execute(await roleService.GetAllAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        Execute(await roleService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken) =>
        Execute(await roleService.CreateAsync(request, cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateRoleRequest request, CancellationToken cancellationToken) =>
        Execute(await roleService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        Execute(await roleService.DeleteAsync(id, cancellationToken));
}
