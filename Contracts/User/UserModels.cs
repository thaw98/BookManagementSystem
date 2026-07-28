namespace Contracts.User;

using System.ComponentModel.DataAnnotations;

public record class UserListDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public long RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record class UserDetailDto : UserListDto
{
    public DateTime UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}

public record class CreateUserRequest
{
    [Required]
    [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = "";

    [Required]
    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    [Required]
    public long RoleId { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}

public record class UpdateUserRequest
{
    [Required]
    [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = "";

    [Required]
    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = "";

    [Required]
    public long RoleId { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;
}

public record class ResetPasswordRequest
{
    [Required]
    public string Password { get; set; } = "";
}

public record class UserFilterRequest : Shared.Models.OffsetPagedRequest
{
    public string? Email { get; set; }

    public long? RoleId { get; set; }
}
