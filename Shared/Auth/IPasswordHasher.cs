namespace Shared.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string passwordHash, string password);
}
