using BookManagementSystem.Domain.Authorization;
using Contracts.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.BookFeature;

[Authorize(Policy = PermissionProvider.LibrarianOnly)]
public sealed class BookController(
    IBookService bookService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] BookFilterRequest filter,
        CancellationToken cancellationToken) =>
        Execute(await bookService.GetAllAsync(
            filter,
            cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await bookService.GetByIdAsync(
            id,
            cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateBookRequest request,
        CancellationToken cancellationToken) =>
        Execute(await bookService.CreateAsync(
            request,
            cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateBookRequest request,
        CancellationToken cancellationToken) =>
        Execute(await bookService.UpdateAsync(
            id,
            request,
            cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await bookService.DeleteAsync(
            id,
            cancellationToken));
}