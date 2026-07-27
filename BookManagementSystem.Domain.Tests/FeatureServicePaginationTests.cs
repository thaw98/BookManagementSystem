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

public sealed class FeatureServicePaginationTests
{
    [Fact]
    public async Task EveryService_ReturnsItsDeterministicallyOrderedSlice()
    {
        await using var db = ServiceTestContext.Create();
        var authors = new[]
        {
            new Author { Id = 2, Name = "Same" },
            new Author { Id = 1, Name = "Same" },
            new Author { Id = 3, Name = "Alpha" }
        };
        var categories = new[]
        {
            new Category { Id = 12, Name = "Same" },
            new Category { Id = 11, Name = "Same" },
            new Category { Id = 13, Name = "Alpha" }
        };
        var roles = new[]
        {
            new Role { Id = 9, Name = "Role 9" },
            new Role { Id = 4, Name = "Role 4" }
        };

        db.AddRange(authors);
        db.AddRange(categories);
        db.AddRange(roles);
        db.Users.AddRange(
            new User { Id = 5, Email = "z@example.com", PasswordHash = "hash", RoleId = 4 },
            new User { Id = 7, Email = "a@example.com", PasswordHash = "hash", RoleId = 4 },
            new User { Id = 6, Email = "a@example.com", PasswordHash = "hash", RoleId = 9 });
        db.Books.AddRange(
            new Book { Id = 8, Title = "Beta", AuthorId = 1, CategoryId = 11, TotalCopies = 1, AvailableCopies = 1 },
            new Book { Id = 10, Title = "Alpha", AuthorId = 1, CategoryId = 11, TotalCopies = 1, AvailableCopies = 1 },
            new Book { Id = 9, Title = "Alpha", AuthorId = 2, CategoryId = 12, TotalCopies = 1, AvailableCopies = 1 });
        await db.SaveChangesAsync();

        var authorPage = (await new AuthorService(db).GetPagedAsync(
            new AuthorFilterRequest { Offset = 1, PageSize = 1 },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(authorPage.Items).Id);

        var categoryPage = (await new CategoryService(db).GetPagedAsync(
            new CategoryFilterRequest { Offset = 1, PageSize = 1 },
            CancellationToken.None)).Data!;
        Assert.Equal(11L, Assert.Single(categoryPage.Items).Id);

        var rolePage = (await new RoleService(db).GetPagedAsync(
            new RoleFilterRequest { Offset = 1, PageSize = 1 },
            CancellationToken.None)).Data!;
        Assert.Equal(9L, Assert.Single(rolePage.Items).Id);

        var userPage = (await ServiceTestContext.CreateUserService(db).GetPagedAsync(
            new UserFilterRequest { Offset = 1, PageSize = 1 },
            CancellationToken.None)).Data!;
        Assert.Equal(7L, Assert.Single(userPage.Items).Id);

        var bookPage = (await new BookService(db).GetPagedAsync(
            new BookFilterRequest { Offset = 1, PageSize = 1 },
            CancellationToken.None)).Data!;
        Assert.Equal(10L, Assert.Single(bookPage.Items).Id);
    }

    [Fact]
    public async Task FeatureFilters_AreAppliedBeforePagingAndCounting()
    {
        await using var db = ServiceTestContext.Create();
        db.Authors.AddRange(
            new Author { Id = 1, Name = "Ada Lovelace" },
            new Author { Id = 2, Name = "Grace Hopper" });
        db.Categories.AddRange(
            new Category { Id = 1, Name = "Computer Science" },
            new Category { Id = 2, Name = "Fiction" });
        db.Roles.AddRange(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Librarian" });
        db.Users.AddRange(
            new User { Id = 1, Email = "ada@example.com", PasswordHash = "hash", RoleId = 1 },
            new User { Id = 2, Email = "grace@example.com", PasswordHash = "hash", RoleId = 2 });
        db.Books.AddRange(
            new Book { Id = 1, Title = "Programming Guide", AuthorId = 1, CategoryId = 1, TotalCopies = 2, AvailableCopies = 2 },
            new Book { Id = 2, Title = "Programming Guide", AuthorId = 2, CategoryId = 1, TotalCopies = 2, AvailableCopies = 2 },
            new Book { Id = 3, Title = "Novel", AuthorId = 1, CategoryId = 2, TotalCopies = 2, AvailableCopies = 2 });
        await db.SaveChangesAsync();

        var authorPage = (await new AuthorService(db).GetPagedAsync(
            new AuthorFilterRequest { Name = "Ada" },
            CancellationToken.None)).Data!;
        Assert.Equal("Ada Lovelace", Assert.Single(authorPage.Items).Name);
        Assert.Equal(1, authorPage.TotalCount);

        var categoryPage = (await new CategoryService(db).GetPagedAsync(
            new CategoryFilterRequest { Name = "Science" },
            CancellationToken.None)).Data!;
        Assert.Equal("Computer Science", Assert.Single(categoryPage.Items).Name);

        var rolePage = (await new RoleService(db).GetPagedAsync(
            new RoleFilterRequest { Name = "Librar" },
            CancellationToken.None)).Data!;
        Assert.Equal("Librarian", Assert.Single(rolePage.Items).Name);

        var userPage = (await ServiceTestContext.CreateUserService(db).GetPagedAsync(
            new UserFilterRequest { Email = "grace", RoleId = 2 },
            CancellationToken.None)).Data!;
        Assert.Equal("grace@example.com", Assert.Single(userPage.Items).Email);
        Assert.Equal(1, userPage.TotalCount);

        var bookPage = (await new BookService(db).GetPagedAsync(
            new BookFilterRequest
            {
                Title = "Guide",
                Author = "Ada",
                CategoryId = 1
            },
            CancellationToken.None)).Data!;
        Assert.Equal(1L, Assert.Single(bookPage.Items).Id);
        Assert.Equal(1, bookPage.TotalCount);
    }

    [Fact]
    public async Task LegacyUnpagedServices_StillReturnCompleteCollections()
    {
        await using var db = ServiceTestContext.Create();
        db.Authors.AddRange(Enumerable.Range(1, 12).Select(index =>
            new Author { Id = index, Name = $"Author {index:00}" }));
        db.Categories.AddRange(Enumerable.Range(1, 12).Select(index =>
            new Category { Id = index, Name = $"Category {index:00}" }));
        db.Roles.AddRange(Enumerable.Range(1, 12).Select(index =>
            new Role { Id = index, Name = $"Role {index:00}" }));
        db.Users.AddRange(Enumerable.Range(1, 12).Select(index =>
            new User
            {
                Id = index,
                Email = $"user{index:00}@example.com",
                PasswordHash = "hash",
                RoleId = index
            }));
        db.Books.AddRange(Enumerable.Range(1, 12).Select(index =>
            new Book
            {
                Id = index,
                Title = $"Book {index:00}",
                AuthorId = index,
                CategoryId = index,
                TotalCopies = 1,
                AvailableCopies = 1
            }));
        await db.SaveChangesAsync();

        Assert.Equal(12, (await new AuthorService(db).GetAllAsync(CancellationToken.None)).Data!.Count);
        Assert.Equal(12, (await new CategoryService(db).GetAllAsync(CancellationToken.None)).Data!.Count);
        Assert.Equal(12, (await new RoleService(db).GetAllAsync(CancellationToken.None)).Data!.Count);
        Assert.Equal(12, (await ServiceTestContext.CreateUserService(db).GetAllAsync(CancellationToken.None)).Data!.Count);
        Assert.Equal(12, (await new BookService(db).GetAllAsync(new BookFilterRequest(), CancellationToken.None)).Data!.Count);
    }
}
