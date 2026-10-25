using Non.Application.Common;
using Non.Application.DTOs.Auth.Request;
using Non.Application.DTOs.Auth.Response;

namespace Non.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result> SendCodeAsync(SendCodeRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> VerifyCodeAsync(VerifyCodeRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task<Result> LogoutAllAsync(Guid userId, CancellationToken ct = default);

    Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}