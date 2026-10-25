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
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private const int MaxAttempts = 5;

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ISmsSender _smsSender;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ISmsSender smsSender,
        IJwtTokenGenerator jwtTokenGenerator,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _smsSender = smsSender;
        _jwtTokenGenerator = jwtTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);
        if (phone is null)
            return Result.Failure("Неверный номер телефона.");

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (firstName.Length == 0 || lastName.Length == 0)
            return Result.Failure("Укажите имя и фамилию.");

        var existing = await _userRepository.GetByPhoneAsync(phone, ct);
        if (existing is not null)
            return Result.Failure("Этот номер уже зарегистрирован. Войдите по коду из SMS.", ErrorType.Conflict);

        return await IssueCodeAsync(phone, firstName, lastName, ct);
    }

    public async Task<Result> SendCodeAsync(SendCodeRequest request, CancellationToken ct = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);
        if (phone is null)
            return Result.Failure("Неверный номер телефона.");

        var user = await _userRepository.GetByPhoneAsync(phone, ct);
        if (user is null || !user.IsActive)
            return Result.Failure("Пользователь с таким номером не найден.", ErrorType.NotFound);

        return await IssueCodeAsync(phone, null, null, ct);
    }

    public async Task<Result<AuthResponse>> VerifyCodeAsync(VerifyCodeRequest request, CancellationToken ct = default)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.Phone);
        if (phone is null)
            return Result<AuthResponse>.Failure("Неверный номер телефона.");

        var key = CacheKeys.OtpCode(phone);
        var entry = await _cacheService.GetAsync<OtpEntry>(key);

        if (entry is null)
            return Result<AuthResponse>.Failure("Код истёк или не был запрошен. Запросите новый код.");

        if (entry.Attempts >= MaxAttempts)
        {
            await _cacheService.RemoveAsync(key);
            return Result<AuthResponse>.Failure("Слишком много попыток. Запросите новый код.", ErrorType.TooManyRequests);
        }

        if (!CodesMatch(entry.Code, request.Code))
        {
            entry.Attempts++;

            var remaining = entry.ExpiresAt - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
                await _cacheService.SetAsync(key, entry, remaining);

            return Result<AuthResponse>.Failure("Неверный код.");
        }

        await _cacheService.RemoveAsync(key);

        var user = await _userRepository.GetByPhoneAsync(phone, ct);

        if (entry.IsRegistration)
        {
            if (user is not null)
                return Result<AuthResponse>.Failure("Этот номер уже зарегистрирован. Войдите по коду из SMS.", ErrorType.Conflict);

            user = new User
            {
                FirstName = entry.FirstName!,
                LastName = entry.LastName!,
                Phone = phone,
                Role = UserRole.Shop,
                IsActive = true
            };

            await _userRepository.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Пользователь с таким номером не найден.", ErrorType.NotFound);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var refreshToken = CreateRefreshToken(user.Id, now, out var rawRefreshToken);

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.RemoveExpiredAsync(user.Id, now, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(BuildResponse(user, rawRefreshToken, refreshToken.ExpiresAt));
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

    private async Task<Result> IssueCodeAsync(string phone, string? firstName, string? lastName, CancellationToken ct)
    {
        var cooldownKey = CacheKeys.OtpCooldown(phone);
        if (await _cacheService.GetAsync<string>(cooldownKey) is not null)
            return Result.Failure("Повторный код можно запросить через минуту.", ErrorType.TooManyRequests);

        var code = GenerateCode();

        await _cacheService.SetAsync(
            CacheKeys.OtpCode(phone),
            new OtpEntry
            {
                Code = code,
                ExpiresAt = DateTime.UtcNow.Add(CodeLifetime),
                FirstName = firstName,
                LastName = lastName
            },
            CodeLifetime);

        await _cacheService.SetAsync(cooldownKey, "1", ResendCooldown);

        await _smsSender.SendAsync(phone, $"Non: ваш код {code}. Никому не сообщайте его.", ct);

        return Result.Success();
    }

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
        Role = user.Role.ToString()
    };

    private sealed class OtpEntry
    {
        public string Code { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public bool IsRegistration => FirstName is not null && LastName is not null;
    }
}