using System.Collections.Concurrent;

namespace WebApp.Services;

public sealed record TokenEntry(string AccessToken, string RefreshToken);

public sealed class TokenStorageService
{
    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public TokenEntry? Get(string sessionId) => _tokens.TryGetValue(sessionId, out var entry) ? entry : null;
    public void Set(string sessionId, TokenEntry entry) => _tokens[sessionId] = entry;
    public bool Exists(string sessionId) => _tokens.ContainsKey(sessionId);

    public void Remove(string sessionId)
    {
        _tokens.TryRemove(sessionId, out _);
        if (_locks.TryRemove(sessionId, out var gate))
            gate.Dispose();
    }

    public SemaphoreSlim GetRefreshLock(string sessionId) =>
        _locks.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
}
