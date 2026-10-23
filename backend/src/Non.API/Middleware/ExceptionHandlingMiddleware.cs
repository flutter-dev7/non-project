using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Non.Application.Common;
using Non.Domain.Common;
using Non.Domain.Enums;
using Npgsql;

namespace Non.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Ошибка после начала отправки ответа: {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, result) = ex switch
        {
            DomainException => (
                StatusCodes.Status400BadRequest,
                Result.Failure(ex.Message, ErrorType.Validation)),

            DbUpdateException { InnerException: PostgresException pg }
                when pg.SqlState == PostgresErrorCodes.UniqueViolation => (
                StatusCodes.Status409Conflict,
                Result.Failure("Такая запись уже существует.", ErrorType.Conflict)),

            DbUpdateException { InnerException: PostgresException pg }
                when pg.SqlState == PostgresErrorCodes.ForeignKeyViolation => (
                StatusCodes.Status409Conflict,
                Result.Failure("Операция невозможна: запись связана с другими данными.", ErrorType.Conflict)),

            _ => (
                StatusCodes.Status500InternalServerError,
                Result.Failure(
                    _environment.IsDevelopment() ? ex.Message : "Внутренняя ошибка сервера.",
                    ErrorType.Failure))
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(ex, "Необработанное исключение: {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Ошибка запроса {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(result, JsonOptions, context.RequestAborted);
    }
}