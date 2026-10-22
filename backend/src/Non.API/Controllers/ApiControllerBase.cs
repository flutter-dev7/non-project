using Microsoft.AspNetCore.Mvc;
using Non.Application.Common;

namespace Non.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Ok(result.Value) : ToErrorResult(result);
    }

    protected IActionResult ToErrorResult(Result result)
    {
        var body = new { error = result.Error };

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(body),
            ErrorType.Conflict => Conflict(body),
            ErrorType.TooManyRequests => StatusCode(StatusCodes.Status429TooManyRequests, body),
            _ => BadRequest(body)
        };
    }
}