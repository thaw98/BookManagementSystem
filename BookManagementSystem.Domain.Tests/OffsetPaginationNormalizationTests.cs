using BookManagementSystem.Domain.Features.AuthorFeature;
using Contracts.Author;
using Database.AppDbContextModels;

namespace BookManagementSystem.Domain.Tests;

public sealed class OffsetPaginationNormalizationTests
{
    [Fact]
    public async Task Requests_AreNormalized_AndCountIsTakenBeforeSlicing()
    {
        await using var db = ServiceTestContext.Create();
        db.Authors.AddRange(Enumerable.Range(1, 150).Select(index =>
            new Author
            {
                Id = index,
                Name = $"Author {index:000}"
            }));
        await db.SaveChangesAsync();

        var service = new AuthorService(db);

        var defaultPage = (await service.GetPagedAsync(
            new AuthorFilterRequest(),
            CancellationToken.None)).Data!;
        Assert.Equal(10, defaultPage.PageSize);
        Assert.Equal(0, defaultPage.Offset);
        Assert.Equal(10, defaultPage.Items.Count);
        Assert.Equal(150, defaultPage.TotalCount);

        var negativePage = (await service.GetPagedAsync(
            new AuthorFilterRequest { Offset = -20, PageSize = -5 },
            CancellationToken.None)).Data!;
        Assert.Equal(0, negativePage.Offset);
        Assert.Equal(10, negativePage.PageSize);
        Assert.Equal(10, negativePage.Items.Count);

        var oversizedPage = (await service.GetPagedAsync(
            new AuthorFilterRequest { PageSize = 500 },
            CancellationToken.None)).Data!;
        Assert.Equal(100, oversizedPage.PageSize);
        Assert.Equal(100, oversizedPage.Items.Count);

        var emptyPage = (await service.GetPagedAsync(
            new AuthorFilterRequest
            {
                Name = "not present",
                Offset = 500,
                PageSize = 25
            },
            CancellationToken.None)).Data!;
        Assert.Equal(0, emptyPage.Offset);
        Assert.Equal(0, emptyPage.TotalCount);
        Assert.Equal(25, emptyPage.PageSize);
        Assert.Empty(emptyPage.Items);
    }

    [Fact]
    public async Task Offset_IsClamped_WhenDeletionRemovesTheLastPage()
    {
        await using var db = ServiceTestContext.Create();
        db.Authors.AddRange(Enumerable.Range(1, 21).Select(index =>
            new Author
            {
                Id = index,
                Name = $"Author {index:00}"
            }));
        await db.SaveChangesAsync();

        var service = new AuthorService(db);
        var lastPage = (await service.GetPagedAsync(
            new AuthorFilterRequest { Offset = 20, PageSize = 10 },
            CancellationToken.None)).Data!;
        Assert.Equal(20, lastPage.Offset);
        Assert.Single(lastPage.Items);

        var deleteResult = await service.DeleteAsync(
            lastPage.Items[0].Id,
            CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var clampedPage = (await service.GetPagedAsync(
            new AuthorFilterRequest { Offset = 20, PageSize = 10 },
            CancellationToken.None)).Data!;
        Assert.Equal(10, clampedPage.Offset);
        Assert.Equal(20, clampedPage.TotalCount);
        Assert.Equal(10, clampedPage.Items.Count);
    }
}
