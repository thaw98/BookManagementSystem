using BookManagementSystem.Domain.Features.AuthorFeature;
using BookManagementSystem.Domain.Features.BookFeature;
using BookManagementSystem.Domain.Features.CategoryFeature;
using BookManagementSystem.Domain.Features.RoleFeature;
using Contracts.Author;
using Contracts.Book;
using Contracts.Category;
using Contracts.Role;
using Contracts.User;
using Database.AppDbContextModels;
using Shared.Models;

namespace BookManagementSystem.Domain.Tests;

public sealed class PagePaginationTests
{
    [Fact]
    public void SharedPagingModels_NormalizeValuesAndExposeNavigationMetadata()
    {
        var request = new OffsetPagedRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(10, request.PageSize);

        request.Page = -5;
        request.PageSize = 0;
        Assert.Equal(1, request.Page);
        Assert.Equal(1, request.PageSize);

        request.PageSize = 500;
        Assert.Equal(100, request.PageSize);

        var result = new OffsetPagedResult<int>
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 21
        };

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task Paging_ClampsToFinalPageAndReturnsPageOneWhenEmpty()
    {
        await using var db = ServiceTestContext.Create();
        db.Authors.AddRange(Enumerable.Range(1, 21).Select(index =>
            new Author { Id = index, Name = $"Author {index:00}" }));
        await db.SaveChangesAsync();

        var service = new AuthorService(db);
        var finalPage = (await service.GetPagedAsync(
            new AuthorFilterRequest { Page = 500, PageSize = 10 },
            CancellationToken.None)).Data!;

        Assert.Equal(3, finalPage.Page);
        Assert.Equal(3, finalPage.TotalPages);
        Assert.Equal(21, finalPage.TotalCount);
        Assert.Single(finalPage.Items);
        Assert.True(finalPage.HasPrevious);
        Assert.False(finalPage.HasNext);

        var emptyPage = (await service.GetPagedAsync(
            new AuthorFilterRequest
            {
                Page = 500,
                PageSize = 25,
                Search = "not present"
            },
            CancellationToken.None)).Data!;

        Assert.Equal(1, emptyPage.Page);
        Assert.Equal(25, emptyPage.PageSize);
        Assert.Equal(0, emptyPage.TotalCount);
        Assert.Equal(0, emptyPage.TotalPages);
        Assert.Empty(emptyPage.Items);
        Assert.False(emptyPage.HasPrevious);
        Assert.False(emptyPage.HasNext);
    }

    [Fact]
    public async Task SharedSearch_IsCombinedWithFeatureFilters()
    {
        await using var db = ServiceTestContext.Create();
        db.Authors.AddRange(
            new Author { Id = 1, Name = "Ada Lovelace" },
            new Author { Id = 2, Name = "Grace Hopper" });
        db.Categories.AddRange(
            new Category { Id = 1, Name = "Computer Science" },
            new Category { Id = 2, Name = "Historical Fiction" });
        db.Roles.AddRange(
            new Role { Id = 1, Name = "System Admin" },
            new Role { Id = 2, Name = "Library Member" });
        db.Users.AddRange(
            new User { Id = 1, Email = "ada@example.com", PasswordHash = "hash", RoleId = 1 },
            new User { Id = 2, Email = "grace@example.com", PasswordHash = "hash", RoleId = 2 });
        db.Books.AddRange(
            new Book { Id = 1, Title = "Programming Guide", AuthorId = 1, CategoryId = 1, TotalCopies = 1, AvailableCopies = 1 },
            new Book { Id = 2, Title = "Ada Biography", AuthorId = 2, CategoryId = 2, TotalCopies = 1, AvailableCopies = 1 });
        await db.SaveChangesAsync();

        var author = (await new AuthorService(db).GetPagedAsync(
            new AuthorFilterRequest { Name = "Ada", Search = "Lovelace" },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(author.Items).Id);

        var category = (await new CategoryService(db).GetPagedAsync(
            new CategoryFilterRequest { Name = "Computer", Search = "Science" },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(category.Items).Id);

        var role = (await new RoleService(db).GetPagedAsync(
            new RoleFilterRequest { Name = "System", Search = "Admin" },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(role.Items).Id);

        var user = (await ServiceTestContext.CreateUserService(db).GetPagedAsync(
            new UserFilterRequest { Email = "example", Search = "ada", RoleId = 1 },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(user.Items).Id);

        var book = (await new BookService(db).GetPagedAsync(
            new BookFilterRequest
            {
                Search = "Ada",
                Title = "Guide",
                Author = "Lovelace",
                CategoryId = 1
            },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(book.Items).Id);
        Assert.Equal(1, book.TotalCount);
    }
}
