using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Contracts;
using Contracts.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebApp.Services;

public sealed class ApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, TokenStorageService tokenStorage)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<Result<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Get, url, null, cancellationToken);

    public Task<Result<T>> PostAsync<T>(string url, object? body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, url, body, cancellationToken);

    public Task<Result<T>> PutAsync<T>(string url, object? body, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, url, body, cancellationToken);

    public Task<Result<T>> DeleteAsync<T>(string url, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, url, null, cancellationToken);

    private async Task<Result<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var response = await SendCoreAsync(method, url, body, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(cancellationToken))
            response = await SendCoreAsync(method, url, body, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<Result<T>>(JsonOptions, cancellationToken);
        return result ?? Result<T>.Error($"Empty API response ({(int)response.StatusCode}).");
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        var sessionId = httpContextAccessor.HttpContext?.User.FindFirst("sid")?.Value;
        if (!string.IsNullOrWhiteSpace(sessionId) && tokenStorage.Get(sessionId) is { } entry)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", entry.AccessToken);

        return await httpClientFactory.CreateClient("BmsApi").SendAsync(request, cancellationToken);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        var sessionId = context?.User.FindFirst("sid")?.Value;
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        var gate = tokenStorage.GetRefreshLock(sessionId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var entry = tokenStorage.Get(sessionId);
            if (entry is null)
                return false;

            var response = await httpClientFactory.CreateClient("BmsApi")
                .PostAsJsonAsync("api/Auth/Refresh", new RefreshRequest { RefreshToken = entry.RefreshToken }, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TokenResponse>>(JsonOptions, cancellationToken);
            if (result?.IsSuccess == true && result.Data is not null)
            {
                tokenStorage.Set(sessionId, new TokenEntry(result.Data.AccessToken, result.Data.RefreshToken));
                return true;
            }

            tokenStorage.Remove(sessionId);
            if (context is not null)
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return false;
        }
        finally
        {
            gate.Release();
        }
    }
}
