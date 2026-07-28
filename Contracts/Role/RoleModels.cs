namespace Contracts.Role;

using System.ComponentModel.DataAnnotations;

public record class RoleListDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsProtected { get; set; }
}

public record class RoleDetailDto : RoleListDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}

public record class CreateRoleRequest
{
    [Required]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
    public string Name { get; set; } = "";

    [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
    public string? Description { get; set; }
}

public record class UpdateRoleRequest
{
    [Required]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters.")]
    public string Name { get; set; } = "";

    [MaxLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
    public string? Description { get; set; }
}

public record class RoleFilterRequest : Shared.Models.OffsetPagedRequest
{
    public string? Name { get; set; }
}
