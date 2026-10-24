using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Non.API.Http;
using Non.API.Middleware;
using Non.API.OpenApi;
using Non.Application.Common;
using Non.Application.Interfaces.Services;
using Non.Domain.Enums;
using Non.Infrastructure;
using Non.Infrastructure.Persistence.Data;
using Non.Infrastructure.Persistence.Seeds;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Некорректное значение." : e.ErrorMessage);

            return new BadRequestObjectResult(
                Result<object>.Failure(string.Join(" ", errors), ErrorType.Validation));
        };
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Section 'Jwt' not found.");

if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
    throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                var role = context.Principal?.FindFirst("role")?.Value;

                if (!Guid.TryParse(subject, out var userId) || string.IsNullOrEmpty(role))
                {
                    context.Fail("Invalid token claims.");
                    return;
                }

                var access = context.HttpContext.RequestServices.GetRequiredService<IUserAccessService>();

                if (!await access.IsAllowedAsync(userId, role, context.HttpContext.RequestAborted))
                    context.Fail("User is inactive or role has changed.");
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();

                var message = context.AuthenticateFailure is null
                    ? "Требуется авторизация."
                    : "Сессия недействительна. Войдите заново.";

                await ApiResponses.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, message, ErrorType.Unauthorized);
            },
            OnForbidden = context => ApiResponses.WriteAsync(
                context.HttpContext, StatusCodes.Status403Forbidden, "Недостаточно прав.", ErrorType.Forbidden)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, _) =>
    {
        await ApiResponses.WriteAsync(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            "Слишком много запросов. Попробуйте позже.",
            ErrorType.TooManyRequests);
    };

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Non API v1");
        options.DocumentTitle = "Non API";
    });
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}