using Microsoft.AspNetCore.Components;
using MudBlazor;
using Shared.Models;

namespace WebApp.Components.Base;

public abstract class ServerPagedComponentBase<TItem> : ComponentBase
{
    private long _requestVersion;

    protected MudTable<TItem>? Table { get; set; }

    protected bool IsLoading { get; private set; }

    protected int LoadedItemCount { get; private set; }

    protected abstract Task<Result<OffsetPagedResult<TItem>>> FetchPageAsync(
        TableState state,
        CancellationToken cancellationToken);

    protected virtual void OnPageLoadSucceeded()
    {
    }

    protected virtual void OnPageLoadFailed(string message)
    {
    }

    protected async Task<TableData<TItem>> LoadServerDataAsync(
        TableState state,
        CancellationToken cancellationToken)
    {
        var requestVersion = Interlocked.Increment(ref _requestVersion);
        IsLoading = true;

        try
        {
            var result = await FetchPageAsync(state, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (requestVersion != Volatile.Read(ref _requestVersion))
                throw new OperationCanceledException(
                    "A newer table request superseded this response.",
                    cancellationToken);

            if (result.IsSuccess && result.Data is not null)
            {
                LoadedItemCount = result.Data.Items.Count;
                OnPageLoadSucceeded();
                return new TableData<TItem>
                {
                    Items = result.Data.Items,
                    TotalItems = result.Data.TotalCount
                };
            }

            LoadedItemCount = 0;
            OnPageLoadFailed(result.Message);
            return EmptyTableData();
        }
        finally
        {
            if (requestVersion == Volatile.Read(ref _requestVersion))
                IsLoading = false;
        }
    }

    protected async Task ReloadFromFirstPageAsync()
    {
        if (Table is null)
            return;

        if (Table.CurrentPage == 0)
            await Table.ReloadServerData();
        else
            Table.NavigateTo(0);
    }

    protected Task ReloadCurrentPageAsync() =>
        Table?.ReloadServerData() ?? Task.CompletedTask;

    protected async Task ReloadAfterDeleteAsync()
    {
        if (Table is null)
            return;

        if (LoadedItemCount == 1 && Table.CurrentPage > 0)
            Table.NavigateTo(Table.CurrentPage - 1);
        else
            await Table.ReloadServerData();
    }

    protected static void AddSortingParameters(
        ICollection<string> queryParameters,
        TableState state)
    {
        if (string.IsNullOrWhiteSpace(state.SortLabel) ||
            state.SortDirection == SortDirection.None)
        {
            return;
        }

        queryParameters.Add($"sortBy={Uri.EscapeDataString(state.SortLabel)}");
        queryParameters.Add(
            $"sortDescending={state.SortDirection == SortDirection.Descending}");
    }

    private static TableData<TItem> EmptyTableData() => new()
    {
        Items = [],
        TotalItems = 0
    };
}
