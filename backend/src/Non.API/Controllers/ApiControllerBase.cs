using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Non.Application.Common;
using Non.Domain.Enums;

namespace Non.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
    
    protected CurrentUser Caller
    {
        get
        {
            var role = Enum.TryParse<UserRole>(User.FindFirstValue("role"), out var parsed) ? parsed : default;

            return new CurrentUser(CurrentUserId, role);
        }
    }

    protected IActionResult ToActionResult(Result result)
    {
        return StatusCode(GetStatusCode(result), result);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        return StatusCode(GetStatusCode(result), result);
    }

    protected IActionResult ToErrorResult(Result result)
    {
        return StatusCode(GetStatusCode(result), result);
    }

    private static int GetStatusCode(Result result)
    {
        if (result.IsSuccess)
            return StatusCodes.Status200OK;

        return result.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}