using BookManagementSystem.Domain.Authorization;
using Contracts.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.AuditLogFeature;

[Authorize(Policy = PermissionProvider.AdminOnly)]
public sealed class AuditLogController(IAuditLogService auditLogService) : BaseController
{
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] AuditLogFilterRequest request,
        CancellationToken cancellationToken) =>
        Execute(await auditLogService.GetPagedAsync(request, cancellationToken));

    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken cancellationToken) =>
        Execute(await auditLogService.GetFilterOptionsAsync(cancellationToken));
}
