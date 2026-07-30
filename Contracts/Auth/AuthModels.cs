namespace Contracts.Auth;

using System.ComponentModel.DataAnnotations;

public static class AuthFailureCodes
{
    public const string DeletedAccount = "account_deleted";
}

public record class LoginRequest
{
    [Required]
    [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public record class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

public record class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

public record class TokenResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public long UserId { get; set; }
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string FullName { get; set; } = "";
}
