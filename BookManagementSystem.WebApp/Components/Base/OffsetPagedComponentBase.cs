using Shared.Models;
using Microsoft.AspNetCore.Components;

namespace WebApp.Components.Base;

public abstract class OffsetPagedComponentBase<TItem> : ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource? _requestCancellation;
    private CancellationTokenSource? _debounceCancellation;
    private long _requestVersion;
    private bool _disposed;

    protected int PageSize { get; private set; } = 10;

    protected int TotalCount { get; private set; }

    protected int CurrentPage { get; private set; } = 1;

    protected int PageCount => Math.Max(
        1,
        (int)Math.Ceiling(TotalCount / (double)PageSize));

    protected bool IsLoading { get; private set; }

    protected List<TItem> PageItems { get; private set; } = [];

    protected abstract Task<Result<OffsetPagedResult<TItem>>> FetchPageAsync(
        CancellationToken cancellationToken);

    protected virtual void OnPageLoadSucceeded()
    {
    }

    protected virtual void OnPageLoadFailed(string message)
    {
    }

    protected async Task ReloadCurrentPageAsync()
    {
        if (_disposed)
            return;

        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(
            ref _requestCancellation,
            cancellation);
        previous?.Cancel();
        previous?.Dispose();

        var requestVersion = Interlocked.Increment(ref _requestVersion);
        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await FetchPageAsync(cancellation.Token);

            if (cancellation.IsCancellationRequested ||
                requestVersion != Volatile.Read(ref _requestVersion))
            {
                return;
            }

            if (result.IsSuccess && result.Data is not null)
            {
                ApplyPagingMetadata(result.Data);
                PageItems = result.Data.Items;
                OnPageLoadSucceeded();
            }
            else
            {
                CurrentPage = 1;
                TotalCount = 0;
                PageItems = [];
                OnPageLoadFailed(result.Message);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (requestVersion == Volatile.Read(ref _requestVersion))
            {
                IsLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    protected Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        return ReloadCurrentPageAsync();
    }

    protected async Task ChangePageAsync(int page)
    {
        var normalizedPage = Math.Clamp(page, 1, PageCount);

        if (normalizedPage == CurrentPage)
            return;

        CurrentPage = normalizedPage;
        await ReloadCurrentPageAsync();
    }

    protected async Task ChangePageSizeAsync(int pageSize)
    {
        if (pageSize == PageSize)
            return;

        PageSize = pageSize;
        CurrentPage = 1;
        await ReloadCurrentPageAsync();
    }

    protected async Task DebounceReloadFromFirstPageAsync(
        TimeSpan delay)
    {
        if (_disposed)
            return;

        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(
            ref _debounceCancellation,
            cancellation);
        previous?.Cancel();
        previous?.Dispose();
        Volatile.Read(ref _requestCancellation)?.Cancel();

        try
        {
            await Task.Delay(delay, cancellation.Token);
            await ReloadFromFirstPageAsync();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }

    private void ApplyPagingMetadata(OffsetPagedResult<TItem> page)
    {
        CurrentPage = Math.Max(1, page.Page);
        PageSize = page.PageSize > 0 ? page.PageSize : 10;
        TotalCount = Math.Max(0, page.TotalCount);
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        Interlocked.Increment(ref _requestVersion);

        var requestCancellation = Interlocked.Exchange(
            ref _requestCancellation,
            null);
        requestCancellation?.Cancel();
        requestCancellation?.Dispose();

        var debounceCancellation = Interlocked.Exchange(
            ref _debounceCancellation,
            null);
        debounceCancellation?.Cancel();
        debounceCancellation?.Dispose();

        return ValueTask.CompletedTask;
    }
}
