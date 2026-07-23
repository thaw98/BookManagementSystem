using BookManagementSystem.Domain.Authorization;
using Contracts.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.CategoryFeature;

[Authorize(Policy = PermissionProvider.LibrarianOnly)]
public sealed class CategoryController(
    ICategoryService categoryService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken) =>
        Execute(await categoryService.GetAllAsync(
            cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.GetByIdAsync(
            id,
            cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.CreateAsync(
            request,
            cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.UpdateAsync(
            id,
            request,
            cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken) =>
        Execute(await categoryService.DeleteAsync(
            id,
            cancellationToken));
}