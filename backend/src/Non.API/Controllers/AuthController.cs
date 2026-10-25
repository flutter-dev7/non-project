using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Non.Application.DTOs.Auth.Request;
using Non.Application.Interfaces.Services;

namespace Non.API.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        return ToActionResult(await _authService.RegisterAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("send-code")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SendCode([FromBody] SendCodeRequest request, CancellationToken ct)
    {
        return ToActionResult(await _authService.SendCodeAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("verify")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Verify([FromBody] VerifyCodeRequest request, CancellationToken ct)
    {
        return ToActionResult(await _authService.VerifyCodeAsync(request, ct));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub");

        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        return ToActionResult(await _authService.GetCurrentUserAsync(userId, ct));
    }
    
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        return ToActionResult(await _authService.RefreshAsync(request, ct));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        return ToActionResult(await _authService.LogoutAsync(request, ct));
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        return ToActionResult(await _authService.LogoutAllAsync(CurrentUserId, ct));
    }
}