namespace Database.AppDbContextModels;

public abstract class AuditableEntity : BaseEntity
{
    public bool IsDeleted { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}
