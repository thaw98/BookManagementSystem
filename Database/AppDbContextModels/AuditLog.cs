namespace Database.AppDbContextModels;

public enum AuditAction
{
    Created,
    Updated,
    Deleted
}

public sealed class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public long EntityId { get; set; }
    public string EntityDisplayName { get; set; } = "";
    public AuditAction Action { get; set; }
    public long? ActorUserId { get; set; }
    public string ActorDisplayName { get; set; } = "System";
    public string? ActorEmail { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string ChangeDetailsJson { get; set; } = "{}";
    public bool IsBackfilled { get; set; }
}
