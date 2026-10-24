using Non.Application.Common;
using Non.Domain.Enums;

namespace Non.API.Http;

public static class ApiResponses
{
    public static Task WriteAsync(HttpContext context, int statusCode, string message, ErrorType errorType)
    {
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(Result<object>.Failure(message, errorType));
    }
}