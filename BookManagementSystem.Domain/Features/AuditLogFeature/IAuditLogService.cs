using Contracts.AuditLog;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.AuditLogFeature;

public interface IAuditLogService
{
    Task<Result<OffsetPagedResult<AuditLogDto>>> GetPagedAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken);

    Task<Result<AuditLogFilterOptions>> GetFilterOptionsAsync(
        CancellationToken cancellationToken);
}
