using Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Base;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Execute<T>(Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Success => Ok(result),
            ResultStatus.Validation => BadRequest(result),
            ResultStatus.NotFound => NotFound(result),
            ResultStatus.Duplicate => Conflict(result),
            ResultStatus.Unauthorized => Unauthorized(result),
            ResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
            _ => StatusCode(StatusCodes.Status500InternalServerError, result)
        };
}
