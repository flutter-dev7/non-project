using System.Security.Cryptography;
using System.Text;
using Non.Application.Common;
using Non.Application.DTOs.Auth.Request;
using Non.Application.DTOs.Auth.Response;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Services;

public class AuthService : IAuthService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private const int MaxCodeAttempts = 5;
    private const int MaxFailures = 5;
    private const string InvalidCredentials = "Неверный email или пароль.";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordService _passwords;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IEmailSender emailSender,
        IPasswordService passwords,
        IJwtTokenGenerator jwtTokenGenerator,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _emailSender = emailSender;
        _passwords = passwords;
        _jwtTokenGenerator = jwtTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        if (email is null)
            return Result.Failure("Неверный email.");

        var phone = PhoneNumberNormalizer.Normalize(request.Phone);
        if (phone is null)
            return Result.Failure("Неверный номер телефона.");

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (firstName.Length == 0 || lastName.Length == 0)
            return Result.Failure("Укажите имя и фамилию.");

        var passwordError = PasswordRules.Validate(request.Password);
        if (passwordError is not null)
            return Result.Failure(passwordError);

        var byEmail = await _userRepository.GetTrackedByEmailAsync(email, ct);
        var byPhone = await _userRepository.GetByPhoneAsync(phone, ct);

        if (byPhone is not null && (byEmail is null || byPhone.Id != byEmail.Id))
            return Result.Failure("Этот номер телефона уже зарегистрирован.", ErrorType.Conflict);

        User user;

        if (byEmail is null)
        {
            user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Phone = phone,
                Email = email,
                EmailConfirmed = false,
                PasswordHash = _passwords.Hash(request.Password),
                Role = UserRole.Shop,
                IsActive = true
            };

            await _userRepository.AddAsync(user, ct);
        }
        else if (!byEmail.EmailConfirmed && byEmail.IsActive && byEmail.PasswordHash is not null && byEmail.Role == UserRole.Shop)
        {
            user = byEmail;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Phone = phone;
            user.PasswordHash = _passwords.Hash(request.Password);
        }
        else
        {
            return Result.Failure(
                "Этот email уже зарегистрирован. Если вы не помните пароль, воспользуйтесь восстановлением.",
                ErrorType.Conflict);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await IssueCodeAsync(EmailCodePurpose.EmailVerification, email, user, ct);
    }

    public async Task<Result<AuthResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        if (email is null)
            return Result<AuthResponse>.Failure("Неверный email.");

        var codeResult = await ConsumeCodeAsync(EmailCodePurpose.EmailVerification, email, request.Code);
        if (codeResult.IsFailure)
            return Result<AuthResponse>.Failure(codeResult.Error!, codeResult.ErrorType!.Value);

        var user = await _userRepository.GetTrackedByEmailAsync(email, ct);
        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Пользователь не найден.", ErrorType.NotFound);

        user.EmailConfirmed = true;

        return Result<AuthResponse>.Success(await IssueTokensAsync(user, ct));
    }

    public async Task<Result> ResendVerificationAsync(EmailRequest request, CancellationToken ct = default)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        if (email is null)
            return Result.Failure("Неверный email.");

        var user = await _userRepository.GetByEmailAsync(email, ct);
        var eligible = user is not null && user.IsActive && !user.EmailConfirmed ? user : null;

        return await IssueCodeAsync(EmailCodePurpose.EmailVerification, email, eligible, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        if (email is null)
            return Unauthorized(InvalidCredentials);

        var failuresKey = AuthCacheKeys.LoginFailures(email);

        if (await IsLockedAsync(failuresKey))
        {
            return Result<AuthResponse>.Failure(
                "Слишком много неудачных попыток входа. Попробуйте через 15 минут.", ErrorType.TooManyRequests);
        }

        var user = await _userRepository.GetByEmailAsync(email, ct);
        var passwordOk = _passwords.Verify(user?.PasswordHash, request.Password);

        if (user is null || !passwordOk)
        {
            await RegisterFailureAsync(failuresKey);

            return Unauthorized(InvalidCredentials);
        }

        if (!user.IsActive)
            return Result<AuthResponse>.Failure("Аккаунт отключён. Обратитесь к администратору.", ErrorType.Forbidden);

        if (!user.EmailConfirmed)
            return Result<AuthResponse>.Failure("Email не подтверждён. Введите код из письма.", ErrorType.Forbidden);

        await _cacheService.RemoveAsync(failuresKey);

        return Result<AuthResponse>.Success(await IssueTokensAsync(user, ct));
    }

    public async Task<Result> ForgotPasswordAsync(EmailRequest request, CancellationToken ct = default)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        if (email is null)
            return Result.Failure("Неверный email.");

        var user = await _userRepository.GetByEmailAsync(email, ct);
        var eligible = user is not null && user.IsActive ? user : null;

        return await IssueCodeAsync(EmailCodePurpose.PasswordReset, email, eligible, ct);
    }

    public async Task<Result<ResetTokenResponse>> VerifyResetCodeAsync(
        VerifyResetCodeRequest request, CancellationToken ct = default)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        if (email is null)
            return Result<ResetTokenResponse>.Failure("Неверный email.");

        var codeResult = await ConsumeCodeAsync(EmailCodePurpose.PasswordReset, email, request.Code);
        if (codeResult.IsFailure)
            return Result<ResetTokenResponse>.Failure(codeResult.Error!, codeResult.ErrorType!.Value);

        var user = await _userRepository.GetByEmailAsync(email, ct);
        if (user is null || !user.IsActive)
            return Result<ResetTokenResponse>.Failure("Пользователь не найден.", ErrorType.NotFound);

        var rawToken = RefreshTokenHasher.Generate();
        var expiresAt = _timeProvider.GetUtcNow().UtcDateTime.Add(ResetTokenLifetime);

        await _cacheService.SetAsync(
            AuthCacheKeys.ResetToken(RefreshTokenHasher.Hash(rawToken)),
            new ResetTokenEntry { UserId = user.Id },
            ResetTokenLifetime);

        return Result<ResetTokenResponse>.Success(new ResetTokenResponse
        {
            ResetToken = rawToken,
            ExpiresAt = expiresAt
        });
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var passwordError = PasswordRules.Validate(request.NewPassword);
        if (passwordError is not null)
            return Result.Failure(passwordError);

        var key = AuthCacheKeys.ResetToken(RefreshTokenHasher.Hash(request.ResetToken));
        var entry = await _cacheService.GetAsync<ResetTokenEntry>(key);

        if (entry is null)
            return Result.Failure("Сессия смены пароля истекла или недействительна. Запросите код заново.");

        var user = await _userRepository.GetTrackedByIdAsync(entry.UserId, ct);

        await _cacheService.RemoveAsync(key);

        if (user is null || !user.IsActive)
            return Result.Failure("Пользователь не найден.", ErrorType.NotFound);

        user.PasswordHash = _passwords.Hash(request.NewPassword);
        user.EmailConfirmed = true;

        await RevokeAllAsync(user.Id, _timeProvider.GetUtcNow().UtcDateTime, ct);

        if (user.Email is not null)
            await _cacheService.RemoveAsync(AuthCacheKeys.LoginFailures(user.Email));

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> ChangePasswordAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetTrackedByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Пользователь не найден.", ErrorType.NotFound);

        var failuresKey = AuthCacheKeys.PasswordChangeFailures(userId);

        if (await IsLockedAsync(failuresKey))
        {
            return Result<AuthResponse>.Failure(
                "Слишком много неверных попыток. Попробуйте через 15 минут.", ErrorType.TooManyRequests);
        }

        if (user.PasswordHash is null)
        {
            return Result<AuthResponse>.Failure(
                "Пароль ещё не задан. Воспользуйтесь восстановлением пароля.");
        }

        if (!_passwords.Verify(user.PasswordHash, request.CurrentPassword))
        {
            await RegisterFailureAsync(failuresKey);

            return Result<AuthResponse>.Failure("Неверный текущий пароль.");
        }

        var passwordError = PasswordRules.Validate(request.NewPassword);
        if (passwordError is not null)
            return Result<AuthResponse>.Failure(passwordError);

        if (request.NewPassword == request.CurrentPassword)
            return Result<AuthResponse>.Failure("Новый пароль должен отличаться от текущего.");

        await _cacheService.RemoveAsync(failuresKey);

        user.PasswordHash = _passwords.Hash(request.NewPassword);

        await RevokeAllAsync(user.Id, _timeProvider.GetUtcNow().UtcDateTime, ct);

        return Result<AuthResponse>.Success(await IssueTokensAsync(user, ct));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var stored = await _refreshTokenRepository.GetByHashAsync(RefreshTokenHasher.Hash(request.RefreshToken), ct);
        if (stored is null)
            return Unauthorized("Недействительный токен обновления.");

        if (stored.RevokedAt is not null)
        {
            if (stored.ReplacedByTokenId is not null)
                await RevokeAllAsync(stored.UserId, now, ct);

            return Unauthorized("Сессия недействительна. Войдите заново.");
        }

        if (stored.ExpiresAt <= now)
            return Unauthorized("Сессия истекла. Войдите заново.");

        var user = stored.User;
        if (user is null || !user.IsActive)
            return Unauthorized("Сессия недействительна. Войдите заново.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        var newToken = CreateRefreshToken(user.Id, now, out var rawRefreshToken);

        if (!await _refreshTokenRepository.TryRevokeAsync(stored.Id, newToken.Id, now, ct))
            return Unauthorized("Токен уже использован. Повторите запрос или войдите заново.");

        await _refreshTokenRepository.AddAsync(newToken, ct);
        await _refreshTokenRepository.RemoveExpiredAsync(user.Id, now, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return Result<AuthResponse>.Success(BuildResponse(user, rawRefreshToken, newToken.ExpiresAt));
    }

    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _refreshTokenRepository.GetByHashAsync(RefreshTokenHasher.Hash(request.RefreshToken), ct);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(Guid userId, CancellationToken ct = default)
    {
        await RevokeAllAsync(userId, _timeProvider.GetUtcNow().UtcDateTime, ct);

        return Result.Success();
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null || !user.IsActive)
            return Result<UserResponse>.Failure("Пользователь не найден.", ErrorType.NotFound);

        return Result<UserResponse>.Success(ToResponse(user));
    }

    private async Task<Result> IssueCodeAsync(EmailCodePurpose purpose, string email, User? user, CancellationToken ct)
    {
        var cooldownKey = AuthCacheKeys.Cooldown(purpose, email);

        if (await _cacheService.GetAsync<string>(cooldownKey) is not null)
            return Result.Failure("Повторный код можно запросить через минуту.", ErrorType.TooManyRequests);

        if (user is null)
        {
            await _cacheService.SetAsync(cooldownKey, "1", ResendCooldown);

            return Result.Success();
        }

        var code = GenerateCode();
        var minutes = (int)CodeLifetime.TotalMinutes;

        var (subject, html) = purpose == EmailCodePurpose.EmailVerification
            ? EmailTemplates.Verification(user.FirstName, code, minutes)
            : EmailTemplates.PasswordReset(user.FirstName, code, minutes);

        try
        {
            await _emailSender.SendAsync(email, user.FullName, subject, html, ct);
        }
        catch (EmailDeliveryException)
        {
            return Result.Failure("Не удалось отправить письмо. Попробуйте позже.", ErrorType.Failure);
        }

        await _cacheService.SetAsync(
            AuthCacheKeys.Code(purpose, email),
            new CodeEntry
            {
                Code = code,
                ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime.Add(CodeLifetime)
            },
            CodeLifetime);

        await _cacheService.SetAsync(cooldownKey, "1", ResendCooldown);

        return Result.Success();
    }

    private async Task<Result> ConsumeCodeAsync(EmailCodePurpose purpose, string email, string code)
    {
        var key = AuthCacheKeys.Code(purpose, email);
        var entry = await _cacheService.GetAsync<CodeEntry>(key);

        if (entry is null)
            return Result.Failure("Код истёк или не был запрошен. Запросите новый код.");

        if (entry.Attempts >= MaxCodeAttempts)
        {
            await _cacheService.RemoveAsync(key);

            return Result.Failure("Слишком много попыток. Запросите новый код.", ErrorType.TooManyRequests);
        }

        if (!CodesMatch(entry.Code, code))
        {
            entry.Attempts++;

            var remaining = entry.ExpiresAt - _timeProvider.GetUtcNow().UtcDateTime;
            if (remaining > TimeSpan.Zero)
                await _cacheService.SetAsync(key, entry, remaining);

            return Result.Failure("Неверный код.");
        }

        await _cacheService.RemoveAsync(key);

        return Result.Success();
    }

    private async Task<bool> IsLockedAsync(string key)
    {
        var counter = await _cacheService.GetAsync<FailureCounter>(key);

        return counter is not null && counter.Count >= MaxFailures;
    }

    private async Task RegisterFailureAsync(string key)
    {
        var counter = await _cacheService.GetAsync<FailureCounter>(key) ?? new FailureCounter();
        counter.Count++;

        await _cacheService.SetAsync(key, counter, FailureWindow);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var refreshToken = CreateRefreshToken(user.Id, now, out var rawRefreshToken);

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.RemoveExpiredAsync(user.Id, now, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return BuildResponse(user, rawRefreshToken, refreshToken.ExpiresAt);
    }

    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var active = await _refreshTokenRepository.GetActiveByUserAsync(userId, now, ct);

        foreach (var token in active)
            token.RevokedAt = now;

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static RefreshToken CreateRefreshToken(Guid userId, DateTime now, out string rawToken)
    {
        rawToken = RefreshTokenHasher.Generate();

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = RefreshTokenHasher.Hash(rawToken),
            ExpiresAt = now.Add(RefreshTokenLifetime)
        };
    }

    private AuthResponse BuildResponse(User user, string rawRefreshToken, DateTime refreshExpiresAt)
    {
        var accessToken = _jwtTokenGenerator.Generate(user);

        return new AuthResponse
        {
            AccessToken = accessToken.Value,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = ToResponse(user)
        };
    }

    private static Result<AuthResponse> Unauthorized(string message) =>
        Result<AuthResponse>.Failure(message, ErrorType.Unauthorized);

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static bool CodesMatch(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));

    private static UserResponse ToResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Phone = user.Phone,
        Email = user.Email,
        Role = user.Role.ToString()
    };

    private sealed class CodeEntry
    {
        public string Code { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private sealed class FailureCounter
    {
        public int Count { get; set; }
    }

    private sealed class ResetTokenEntry
    {
        public Guid UserId { get; set; }
    }
}