using BookManagementSystem.Domain.Authorization;
using Contracts.Author;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.AuthorFeature;

[Authorize(Policy = PermissionProvider.LibrarianOnly)]
public sealed class AuthorController(
    IAuthorService authorService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Execute(await authorService.GetAllAsync(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await authorService.GetByIdAsync(
            id,
            cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateAuthorRequest request,
        CancellationToken cancellationToken) =>
        Execute(await authorService.CreateAsync(
            request,
            cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken) =>
        Execute(await authorService.UpdateAsync(
            id,
            request,
            cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await authorService.DeleteAsync(
            id,
            cancellationToken));
}