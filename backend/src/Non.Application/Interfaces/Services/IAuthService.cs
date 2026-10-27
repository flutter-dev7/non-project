using Non.Application.Common;
using Non.Application.DTOs.Auth.Request;
using Non.Application.DTOs.Auth.Response;

namespace Non.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    Task<Result> ResendVerificationAsync(EmailRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<Result> ForgotPasswordAsync(EmailRequest request, CancellationToken ct = default);

    Task<Result<ResetTokenResponse>> VerifyResetCodeAsync(VerifyResetCodeRequest request, CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task<Result> LogoutAllAsync(Guid userId, CancellationToken ct = default);

    Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}