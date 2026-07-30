using BookManagementSystem.Domain.Authorization;
using Contracts.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.BookFeature;

public sealed class BookController(
    IBookService bookService) : BaseController
{
    [Authorize(Policy = PermissionProvider.CatalogRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BookFilterRequest filter,
        CancellationToken cancellationToken) =>
        Execute(await bookService.GetAllAsync(
            filter,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.CatalogRead)]
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] BookFilterRequest filter,
        CancellationToken cancellationToken) =>
        Execute(await bookService.GetPagedAsync(
            filter,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.CatalogRead)]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await bookService.GetByIdAsync(
            id,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateBookRequest request,
        CancellationToken cancellationToken) =>
        Execute(await bookService.CreateAsync(
            request,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateBookRequest request,
        CancellationToken cancellationToken) =>
        Execute(await bookService.UpdateAsync(
            id,
            request,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await bookService.DeleteAsync(
            id,
            cancellationToken));
}