using BookManagementSystem.Domain.Authorization;
using Contracts.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.CategoryFeature;

public sealed class CategoryController(
    ICategoryService categoryService) : BaseController
{
    [Authorize(Policy = PermissionProvider.CatalogRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Execute(await categoryService.GetAllAsync(
            cancellationToken));

    [Authorize(Policy = PermissionProvider.CatalogRead)]
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] CategoryFilterRequest request,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.GetPagedAsync(
            request,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.CatalogRead)]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.GetByIdAsync(
            id,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.CreateAsync(
            request,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.UpdateAsync(
            id,
            request,
            cancellationToken));

    [Authorize(Policy = PermissionProvider.LibrarianOnly)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.DeleteAsync(
            id,
            cancellationToken));
}
