namespace Contracts.Role;

public class RoleListDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsProtected { get; set; }
}

public sealed class RoleDetailDto : RoleListDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class UpdateRoleRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
