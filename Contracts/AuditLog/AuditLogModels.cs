using Shared.Models;

namespace Contracts.AuditLog;

public sealed record AuditLogFilterRequest : OffsetPagedRequest
{
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? Actor { get; set; }
    public DateTime? FromDateLocal { get; set; }
    public DateTime? ToDateLocal { get; set; }
}

public sealed record AuditLogDto
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public long EntityId { get; set; }
    public string EntityDisplayName { get; set; } = "";
    public string Action { get; set; } = "";
    public long? ActorUserId { get; set; }
    public string ActorDisplayName { get; set; } = "";
    public string? ActorEmail { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string ChangeDetailsJson { get; set; } = "{}";
    public bool IsBackfilled { get; set; }
}

public sealed record AuditActorOption
{
    public string Key { get; set; } = "";
    public long? UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
}

public sealed record AuditLogFilterOptions
{
    public List<string> EntityTypes { get; set; } = [];
    public List<string> Actions { get; set; } = [];
    public List<AuditActorOption> Actors { get; set; } = [];
}
