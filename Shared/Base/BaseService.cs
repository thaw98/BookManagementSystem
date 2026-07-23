using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Shared.Base;

public sealed class BaseService(IHttpContextAccessor httpContextAccessor) : IBaseService
{
    public long? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return long.TryParse(value, out var id) ? id : null;
        }
    }
}
