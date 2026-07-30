using Contracts.AuditLog;
using Database.AppDbContextModels;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace BookManagementSystem.Domain.Features.AuditLogFeature;

public sealed class AuditLogService(AppDbContext db) : IAuditLogService
{
    private static readonly TimeZoneInfo MyanmarTimeZone = ResolveMyanmarTimeZone();

    public async Task<Result<OffsetPagedResult<AuditLogDto>>> GetPagedAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.EntityDisplayName.Contains(search) ||
                x.EntityType.Contains(search) ||
                x.ActorDisplayName.Contains(search) ||
                (x.ActorEmail != null && x.ActorEmail.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(x => x.EntityType == request.EntityType);

        if (Enum.TryParse<AuditAction>(request.Action, true, out var action))
            query = query.Where(x => x.Action == action);

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            if (string.Equals(request.Actor, "system", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => x.ActorUserId == null);
            else if (long.TryParse(request.Actor, out var actorId))
                query = query.Where(x => x.ActorUserId == actorId);
        }

        if (request.FromDateLocal.HasValue)
        {
            var fromUtc = LocalBoundaryToUtc(request.FromDateLocal.Value.Date);
            query = query.Where(x => x.OccurredAtUtc >= fromUtc);
        }

        if (request.ToDateLocal.HasValue)
        {
            var toUtcExclusive = LocalBoundaryToUtc(request.ToDateLocal.Value.Date.AddDays(1));
            query = query.Where(x => x.OccurredAtUtc < toUtcExclusive);
        }

        var page = await Pagination.OffsetPagination.CreateAsync(
            query,
            request,
            source => ApplyOrdering(source, request),
            x => new AuditLogDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                EntityDisplayName = x.EntityDisplayName,
                Action = x.Action == AuditAction.Created
                    ? "Created"
                    : x.Action == AuditAction.Updated ? "Updated" : "Deleted",
                ActorUserId = x.ActorUserId,
                ActorDisplayName = x.ActorDisplayName,
                ActorEmail = x.ActorEmail,
                OccurredAtUtc = x.OccurredAtUtc,
                ChangeDetailsJson = x.ChangeDetailsJson,
                IsBackfilled = x.IsBackfilled
            },
            cancellationToken);

        return Result<OffsetPagedResult<AuditLogDto>>.Success(page);
    }

    public async Task<Result<AuditLogFilterOptions>> GetFilterOptionsAsync(
        CancellationToken cancellationToken)
    {
        var entityTypes = await db.AuditLogs.AsNoTracking()
            .Select(x => x.EntityType).Distinct().OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var actorRows = await db.AuditLogs.AsNoTracking()
            .Select(x => new { x.ActorUserId, x.ActorDisplayName, x.ActorEmail, x.OccurredAtUtc, x.Id })
            .OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var actors = actorRows
            .GroupBy(x => x.ActorUserId)
            .Select(x => x.First())
            .Select(x => new AuditActorOption
            {
                Key = x.ActorUserId?.ToString() ?? "system",
                UserId = x.ActorUserId,
                DisplayName = x.ActorDisplayName,
                Email = x.ActorEmail
            })
            .OrderBy(x => x.DisplayName)
            .ToList();

        return Result<AuditLogFilterOptions>.Success(new AuditLogFilterOptions
        {
            EntityTypes = entityTypes,
            Actions = Enum.GetNames<AuditAction>().ToList(),
            Actors = actors
        });
    }

    private static DateTime LocalBoundaryToUtc(DateTime localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified),
            MyanmarTimeZone);

    private static IOrderedQueryable<AuditLog> ApplyOrdering(
        IQueryable<AuditLog> query,
        AuditLogFilterRequest request) =>
        (request.SortBy, request.SortDescending) switch
        {
            ("entity", true) => query.OrderByDescending(x => x.EntityDisplayName).ThenByDescending(x => x.Id),
            ("entity", false) => query.OrderBy(x => x.EntityDisplayName).ThenBy(x => x.Id),
            ("action", true) => query.OrderByDescending(x => x.Action).ThenByDescending(x => x.Id),
            ("action", false) => query.OrderBy(x => x.Action).ThenBy(x => x.Id),
            ("actor", true) => query.OrderByDescending(x => x.ActorDisplayName).ThenByDescending(x => x.Id),
            ("actor", false) => query.OrderBy(x => x.ActorDisplayName).ThenBy(x => x.Id),
            ("occurredAt", false) => query.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
        };

    private static TimeZoneInfo ResolveMyanmarTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Yangon"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time"); }
    }
}
