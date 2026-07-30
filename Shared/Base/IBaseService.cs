namespace Shared.Base;

public interface IBaseService
{
    long? UserId { get; }
    string? UserDisplayName { get; }
    string? UserEmail { get; }
}
