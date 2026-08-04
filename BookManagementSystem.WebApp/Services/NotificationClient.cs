using System.Security.Claims;
using Contracts.Notification;
using Microsoft.AspNetCore.SignalR.Client;
using Shared.Constants;

namespace WebApp.Services;

public sealed class NotificationClient(
    ApiClient api,
    IConfiguration configuration,
    ILogger<NotificationClient> logger) : IAsyncDisposable
{
    private readonly Dictionary<long, NotificationDto> _items = [];
    private HubConnection? _connection;
    private string? _sessionId;
    private bool _started;

    public event Action? StateChanged;
    public IReadOnlyList<NotificationDto> Notifications => _items.Values
        .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList();
    public int UnreadCount { get; private set; }

    public async Task InitializeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (_started || user.Identity?.IsAuthenticated != true) return;
        var role = user.FindFirstValue(ClaimTypes.Role);
        if (role is not (RoleNames.Librarian or RoleNames.LibraryMember)) return;
        _sessionId = user.FindFirstValue("sid");
        if (string.IsNullOrWhiteSpace(_sessionId)) return;
        _started = true;
        await ReloadAsync(cancellationToken);
        try
        {
            var apiBase = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7239");
            _connection = new HubConnectionBuilder()
                .WithUrl(new Uri(apiBase, "/hubs/notifications"), options =>
                    options.AccessTokenProvider = () => api.GetAccessTokenAsync(_sessionId!))
                .WithAutomaticReconnect()
                .Build();
            _connection.On<NotificationDto>("NotificationReceived", Receive);
            _connection.Reconnected += async _ => await ReloadAsync();
            await _connection.StartAsync(cancellationToken);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Notification hub connection could not be started."); }
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await api.GetAsync<NotificationInboxDto>("api/Notification/inbox", cancellationToken);
            if (!result.IsSuccess || result.Data is null) return;
            _items.Clear();
            foreach (var item in result.Data.Notifications) _items[item.Id] = item;
            UnreadCount = result.Data.UnreadCount;
            StateChanged?.Invoke();
        }
        catch (Exception ex) { logger.LogWarning(ex, "Notification inbox could not be reloaded."); }
    }

    public async Task<bool> MarkReadAsync(long id)
    {
        try
        {
            var result = await api.PutAsync<int>($"api/Notification/{id}/read", null);
            if (!result.IsSuccess) return false;
            if (_items.TryGetValue(id, out var item) && item.ReadAt is null)
                item.ReadAt = DateTime.UtcNow;
            UnreadCount = result.Data;
            StateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notification {NotificationId} could not be marked read.", id);
            return false;
        }
    }

    public async Task<bool> MarkAllReadAsync()
    {
        try
        {
            var result = await api.PutAsync<int>("api/Notification/read-all", null);
            if (!result.IsSuccess) return false;
            var readAt = DateTime.UtcNow;
            foreach (var item in _items.Values.Where(x => x.ReadAt is null)) item.ReadAt = readAt;
            UnreadCount = result.Data;
            StateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notifications could not be marked read.");
            return false;
        }
    }

    private void Receive(NotificationDto item)
    {
        item.ReadAt = null;
        if (_items.TryAdd(item.Id, item)) UnreadCount++;
        StateChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is null) return;
        _connection.Remove("NotificationReceived");
        await _connection.DisposeAsync();
    }
}
