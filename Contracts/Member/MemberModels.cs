namespace Contracts.Member;

public class MemberListDto
{
    public long Id { get; set; }

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class MemberDetailDto : MemberListDto
{
    public DateTime UpdatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public long? UpdatedBy { get; set; }
}

public sealed class CreateMemberRequest
{
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Password { get; set; } = "";

    public string ConfirmPassword { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

public sealed class UpdateMemberRequest
{
    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public bool IsActive { get; set; }
}