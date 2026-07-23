using Microsoft.AspNetCore.Identity;

namespace Shared.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();

    public string HashPassword(string password) => Hasher.HashPassword(new object(), password);

    public bool VerifyPassword(string passwordHash, string password)
    {
        var result = Hasher.VerifyHashedPassword(new object(), passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
