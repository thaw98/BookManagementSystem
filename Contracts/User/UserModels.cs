namespace Contracts.User;

public class UserListDto
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public long RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class UserDetailDto : UserListDto
{
    public DateTime UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public long? UpdatedBy { get; set; }
}

public sealed class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public long RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateUserRequest
{
    public string Email { get; set; } = "";
    public long RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ResetPasswordRequest
{
    public string Password { get; set; } = "";
}
