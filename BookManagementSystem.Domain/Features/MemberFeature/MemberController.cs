using BookManagementSystem.Domain.Authorization;
using Contracts.Member;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Base;

namespace BookManagementSystem.Domain.Features.MemberFeature;

[Authorize(Policy = PermissionProvider.LibrarianOnly)]
public sealed class MemberController(
    IMemberService memberService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        return Execute(
            await memberService.GetAllAsync(
                cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        return Execute(
            await memberService.GetByIdAsync(
                id,
                cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMemberRequest request,
        CancellationToken cancellationToken)
    {
        return Execute(
            await memberService.CreateAsync(
                request,
                cancellationToken));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateMemberRequest request,
        CancellationToken cancellationToken)
    {
        return Execute(
            await memberService.UpdateAsync(
                id,
                request,
                cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        return Execute(
            await memberService.DeleteAsync(
                id,
                cancellationToken));
    }
}