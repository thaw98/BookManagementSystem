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

namespace BookManagementSystem.Domain.Tests;

public sealed class PagedSortingTests
{
    [Theory]
    [InlineData("title", false, "101,102,100,103")]
    [InlineData("title", true, "103,100,101,102")]
    [InlineData("author", false, "101,102,100,103")]
    [InlineData("author", true, "103,100,101,102")]
    [InlineData("category", false, "101,102,100,103")]
    [InlineData("category", true, "103,100,101,102")]
    [InlineData("totalCopies", false, "103,101,102,100")]
    [InlineData("totalCopies", true, "100,101,102,103")]
    [InlineData("availableCopies", false, "103,100,102,101")]
    [InlineData("availableCopies", true, "101,100,102,103")]
    public async Task Books_SortEverySupportedKeyInBothDirections(
        string sortBy,
        bool descending,
        string expectedIds)
    {
        await using var db = await CreateSortingContextAsync();

        var page = (await new BookService(db).GetPagedAsync(
            new BookFilterRequest
            {
                PageSize = 10,
                SortBy = sortBy,
                SortDescending = descending
            },
            CancellationToken.None)).Data!;

        Assert.Equal(ParseIds(expectedIds), page.Items.Select(x => x.Id));
    }

    [Theory]
    [InlineData(false, "2,3,1,4")]
    [InlineData(true, "4,1,2,3")]
    public async Task Authors_SortByNameInBothDirections(
        bool descending,
        string expectedIds)
    {
        await using var db = await CreateSortingContextAsync();

        var page = (await new AuthorService(db).GetPagedAsync(
            new AuthorFilterRequest
            {
                PageSize = 10,
                SortBy = "name",
                SortDescending = descending
            },
            CancellationToken.None)).Data!;

        Assert.Equal(ParseIds(expectedIds), page.Items.Select(x => x.Id));
    }

    [Theory]
    [InlineData(false, "11,12,10,13")]
    [InlineData(true, "13,10,11,12")]
    public async Task Categories_SortByNameInBothDirections(
        bool descending,
        string expectedIds)
    {
        await using var db = await CreateSortingContextAsync();

        var page = (await new CategoryService(db).GetPagedAsync(
            new CategoryFilterRequest
            {
                PageSize = 10,
                SortBy = "name",
                SortDescending = descending
            },
            CancellationToken.None)).Data!;

        Assert.Equal(ParseIds(expectedIds), page.Items.Select(x => x.Id));
    }

    [Theory]
    [InlineData("name", false, "21,22,20,23")]
    [InlineData("name", true, "23,20,21,22")]
    [InlineData("description", false, "23,20,21,22")]
    [InlineData("description", true, "21,22,20,23")]
    public async Task Roles_SortEverySupportedKeyInBothDirections(
        string sortBy,
        bool descending,
        string expectedIds)
    {
        await using var db = await CreateSortingContextAsync();

        var page = (await new RoleService(db).GetPagedAsync(
            new RoleFilterRequest
            {
                PageSize = 10,
                SortBy = sortBy,
                SortDescending = descending
            },
            CancellationToken.None)).Data!;

        Assert.Equal(ParseIds(expectedIds), page.Items.Select(x => x.Id));
    }

    [Theory]
    [InlineData("email", false, "31,32,30,33")]
    [InlineData("email", true, "33,30,31,32")]
    [InlineData("role", false, "31,32,30,33")]
    [InlineData("role", true, "33,30,31,32")]
    [InlineData("active", false, "31,32,30,33")]
    [InlineData("active", true, "30,33,31,32")]
    public async Task Users_SortEverySupportedKeyInBothDirections(
        string sortBy,
        bool descending,
        string expectedIds)
    {
        await using var db = await CreateSortingContextAsync();

        var page = (await ServiceTestContext.CreateUserService(db).GetPagedAsync(
            new UserFilterRequest
            {
                PageSize = 10,
                SortBy = sortBy,
                SortDescending = descending
            },
            CancellationToken.None)).Data!;

        Assert.Equal(ParseIds(expectedIds), page.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task SortingOccursBeforePaginationAndCombinesWithFilters()
    {
        await using var db = await CreateSortingContextAsync();

        var page = (await new BookService(db).GetPagedAsync(
            new BookFilterRequest
            {
                Title = "Alpha",
                Author = "Alpha",
                CategoryId = 11,
                SortBy = "totalCopies",
                SortDescending = true,
                Page = 2,
                PageSize = 1
            },
            CancellationToken.None)).Data!;

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(102L, Assert.Single(page.Items).Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unsupported")]
    public async Task MissingOrUnknownSortKeysUseExistingDefaults(string? sortBy)
    {
        await using var db = await CreateSortingContextAsync();

        var books = (await new BookService(db).GetPagedAsync(
            new BookFilterRequest { PageSize = 10, SortBy = sortBy, SortDescending = true },
            CancellationToken.None)).Data!;
        var authors = (await new AuthorService(db).GetPagedAsync(
            new AuthorFilterRequest { PageSize = 10, SortBy = sortBy, SortDescending = true },
            CancellationToken.None)).Data!;
        var categories = (await new CategoryService(db).GetPagedAsync(
            new CategoryFilterRequest { PageSize = 10, SortBy = sortBy, SortDescending = true },
            CancellationToken.None)).Data!;
        var roles = (await new RoleService(db).GetPagedAsync(
            new RoleFilterRequest { PageSize = 10, SortBy = sortBy, SortDescending = true },
            CancellationToken.None)).Data!;
        var users = (await ServiceTestContext.CreateUserService(db).GetPagedAsync(
            new UserFilterRequest { PageSize = 10, SortBy = sortBy, SortDescending = true },
            CancellationToken.None)).Data!;

        Assert.Equal(new long[] { 101, 102, 100, 103 }, books.Items.Select(x => x.Id));
        Assert.Equal(new long[] { 2, 3, 1, 4 }, authors.Items.Select(x => x.Id));
        Assert.Equal(new long[] { 11, 12, 10, 13 }, categories.Items.Select(x => x.Id));
        Assert.Equal(new long[] { 20, 21, 22, 23 }, roles.Items.Select(x => x.Id));
        Assert.Equal(new long[] { 31, 32, 30, 33 }, users.Items.Select(x => x.Id));
    }

    private static async Task<AppDbContext> CreateSortingContextAsync()
    {
        var db = ServiceTestContext.Create();

        db.Authors.AddRange(
            new Author { Id = 1, Name = "Beta" },
            new Author { Id = 2, Name = "Alpha" },
            new Author { Id = 3, Name = "Alpha" },
            new Author { Id = 4, Name = "Gamma" });
        db.Categories.AddRange(
            new Category { Id = 10, Name = "Y" },
            new Category { Id = 11, Name = "X" },
            new Category { Id = 12, Name = "X" },
            new Category { Id = 13, Name = "Z" });
        db.Roles.AddRange(
            new Role { Id = 20, Name = "Beta", Description = "B" },
            new Role { Id = 21, Name = "Alpha", Description = "C" },
            new Role { Id = 22, Name = "Alpha", Description = "C" },
            new Role { Id = 23, Name = "Gamma", Description = "A" });
        db.Users.AddRange(
            new User { Id = 30, Email = "beta@example.com", PasswordHash = "hash", RoleId = 20, IsActive = true },
            new User { Id = 31, Email = "alpha@example.com", PasswordHash = "hash", RoleId = 21, IsActive = false },
            new User { Id = 32, Email = "alpha@example.com", PasswordHash = "hash", RoleId = 22, IsActive = false },
            new User { Id = 33, Email = "gamma@example.com", PasswordHash = "hash", RoleId = 23, IsActive = true });
        db.Books.AddRange(
            new Book { Id = 100, Title = "Beta", AuthorId = 1, CategoryId = 10, TotalCopies = 3, AvailableCopies = 1 },
            new Book { Id = 101, Title = "Alpha", AuthorId = 2, CategoryId = 11, TotalCopies = 2, AvailableCopies = 2 },
            new Book { Id = 102, Title = "Alpha", AuthorId = 3, CategoryId = 11, TotalCopies = 2, AvailableCopies = 1 },
            new Book { Id = 103, Title = "Gamma", AuthorId = 4, CategoryId = 13, TotalCopies = 1, AvailableCopies = 0 });

        await db.SaveChangesAsync();
        return db;
    }

    private static long[] ParseIds(string value) =>
        value.Split(',').Select(long.Parse).ToArray();
}
