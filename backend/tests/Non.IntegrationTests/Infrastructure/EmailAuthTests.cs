using System.Net;
using Non.Application.DTOs.Auth.Request;
using Non.Application.DTOs.Auth.Response;
using Non.Application.DTOs.Users.Request;
using Non.Application.DTOs.Users.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class EmailAuthTests
{
    private const string Password = "Secret123";
    private const string NewPassword = "Brand9New";

    private readonly ApiFixture _api;

    public EmailAuthTests(ApiFixture api)
    {
        _api = api;
    }

    private static string NewEmail() => $"new-{Guid.NewGuid():N}@test.local";

    private static string NewPhone() => "+9929" + Random.Shared.Next(0, 100_000_000).ToString("D8");

    private Task<ApiResponse<object>> RegisterAsync(string email, string phone, string password = Password) =>
        _api.CreateAnonymousClient().PostAsync<object>("/api/auth/register", new RegisterRequest
        {
            FirstName = "Новый",
            LastName = "Клиент",
            Phone = phone,
            Email = email,
            Password = password
        });

    private Task<ApiResponse<AuthResponse>> LoginAsync(string email, string password) =>
        _api.CreateAnonymousClient().PostAsync<AuthResponse>("/api/auth/login",
            new LoginRequest { Email = email, Password = password });

    private Task<ApiResponse<AuthResponse>> VerifyEmailAsync(string email, string code) =>
        _api.CreateAnonymousClient().PostAsync<AuthResponse>("/api/auth/verify-email",
            new VerifyEmailRequest { Email = email, Code = code });

    private static string WrongCode(string actual) => actual == "000000" ? "111111" : "000000";

    private async Task<string> RequestResetTokenAsync(string email)
    {
        var anonymous = _api.CreateAnonymousClient();

        await _api.ClearAuthThrottlesAsync(email);

        (await anonymous.PostAsync<object>("/api/auth/forgot-password", new EmailRequest { Email = email })).Require();

        var code = _api.Email.GetCode(email);
        Assert.NotNull(code);

        var token = (await anonymous.PostAsync<ResetTokenResponse>("/api/auth/verify-reset-code",
            new VerifyResetCodeRequest { Email = email, Code = code! })).Require();

        return token.ResetToken;
    }

    private Task<ApiResponse<object>> ResetPasswordAsync(string resetToken, string newPassword) =>
        _api.CreateAnonymousClient().PostAsync<object>("/api/auth/reset-password",
            new ResetPasswordRequest { ResetToken = resetToken, NewPassword = newPassword });

    [Fact]
    public async Task Register_ThenVerifyEmail_ReturnsTokensForShopUser()
    {
        var email = NewEmail();
        (await RegisterAsync(email, NewPhone())).Require();

        var beforeVerification = await LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.Forbidden, beforeVerification.StatusCode);

        var code = _api.Email.GetCode(email);
        Assert.NotNull(code);

        var auth = (await VerifyEmailAsync(email, code!)).Require();

        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
        Assert.Equal("Shop", auth.User.Role);
        Assert.Equal(email, auth.User.Email);

        var me = (await _api.CreateClientWithToken(auth.AccessToken).GetAsync<UserResponse>("/api/auth/me")).Require();
        Assert.Equal(auth.User.Id, me.Id);

        var login = (await LoginAsync(email.ToUpperInvariant(), Password)).Require();
        Assert.Equal(auth.User.Id, login.User.Id);
    }

    [Fact]
    public async Task Register_WithWeakPassword_IsRejected()
    {
        var tooShort = await RegisterAsync(NewEmail(), NewPhone(), "abc1");
        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);

        var noDigits = await RegisterAsync(NewEmail(), NewPhone(), "onlyletters");
        Assert.Equal(HttpStatusCode.BadRequest, noDigits.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_IsRejected()
    {
        var response = await RegisterAsync("not-an-email", NewPhone());

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithTakenEmail_ReturnsConflict()
    {
        var existing = await _api.CreateUserAsync(UserRole.Shop);
        var email = await _api.GetEmailAsync(existing.UserId);

        var response = await RegisterAsync(email, NewPhone());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithTakenPhone_ReturnsConflict()
    {
        var existing = await _api.CreateUserAsync(UserRole.Shop);

        var response = await RegisterAsync(NewEmail(), existing.Phone);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_AgainBeforeConfirmation_ReplacesPassword()
    {
        var email = NewEmail();
        var phone = NewPhone();

        (await RegisterAsync(email, phone, "FirstPass1")).Require();
        await _api.ClearAuthThrottlesAsync(email);
        (await RegisterAsync(email, phone, "SecondPass2")).Require();

        var code = _api.Email.GetCode(email);
        Assert.NotNull(code);

        (await VerifyEmailAsync(email, code!)).Require();

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "FirstPass1")).StatusCode);
        Assert.True((await LoginAsync(email, "SecondPass2")).IsSuccess);
    }

    [Fact]
    public async Task Register_AfterConfirmation_CannotBeTakenOver()
    {
        var email = NewEmail();
        var phone = NewPhone();

        (await RegisterAsync(email, phone)).Require();
        (await VerifyEmailAsync(email, _api.Email.GetCode(email)!)).Require();

        await _api.ClearAuthThrottlesAsync(email);
        var takeover = await RegisterAsync(email, NewPhone(), "Attacker123");

        Assert.Equal(HttpStatusCode.Conflict, takeover.StatusCode);
        Assert.True((await LoginAsync(email, Password)).IsSuccess);
    }

    [Fact]
    public async Task VerifyEmail_WithWrongCode_Fails_AndTooManyAttemptsAreBlocked()
    {
        var email = NewEmail();
        (await RegisterAsync(email, NewPhone())).Require();

        var code = _api.Email.GetCode(email)!;
        var wrong = WrongCode(code);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await VerifyEmailAsync(email, wrong);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var blocked = await VerifyEmailAsync(email, code);

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_SendsNewCode_ButRespectsCooldown()
    {
        var email = NewEmail();
        (await RegisterAsync(email, NewPhone())).Require();

        var immediately = await _api.CreateAnonymousClient()
            .PostAsync<object>("/api/auth/resend-verification", new EmailRequest { Email = email });
        Assert.Equal(HttpStatusCode.TooManyRequests, immediately.StatusCode);

        await _api.ClearAuthThrottlesAsync(email);

        (await _api.CreateAnonymousClient()
            .PostAsync<object>("/api/auth/resend-verification", new EmailRequest { Email = email })).Require();

        Assert.Equal(2, _api.Email.SentCount(email));
    }

    [Fact]
    public async Task ResendVerification_ForUnknownEmail_LooksTheSame_AndSendsNothing()
    {
        var email = NewEmail();
        var anonymous = _api.CreateAnonymousClient();

        (await anonymous.PostAsync<object>("/api/auth/resend-verification", new EmailRequest { Email = email })).Require();

        var second = await anonymous.PostAsync<object>("/api/auth/resend-verification", new EmailRequest { Email = email });

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(0, _api.Email.SentCount(email));
    }

    [Fact]
    public async Task Register_WhenEmailDeliveryFails_ReturnsError_AndCodeCanBeResent()
    {
        var email = NewEmail();

        _api.Email.FailNext = true;
        var failed = await RegisterAsync(email, NewPhone());

        Assert.False(failed.IsSuccess);
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);

        (await _api.CreateAnonymousClient()
            .PostAsync<object>("/api/auth/resend-verification", new EmailRequest { Email = email })).Require();

        var code = _api.Email.GetCode(email);
        Assert.NotNull(code);

        Assert.True((await VerifyEmailAsync(email, code!)).IsSuccess);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401_AndSameMessageAsUnknownEmail()
    {
        var user = await _api.CreateUserAsync(UserRole.Courier);
        var email = await _api.GetEmailAsync(user.UserId);

        var wrongPassword = await LoginAsync(email, "WrongPass1");
        var unknownEmail = await LoginAsync(NewEmail(), "WrongPass1");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
    }

    [Fact]
    public async Task Login_IsLockedAfterFiveFailures_UntilThrottleExpires()
    {
        var user = await _api.CreateUserAsync(UserRole.Courier);
        var email = await _api.GetEmailAsync(user.UserId);
        await _api.ClearAuthThrottlesAsync(email);

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "WrongPass1")).StatusCode);

        var locked = await LoginAsync(email, ApiFixture.TestPassword);
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);

        await _api.ClearAuthThrottlesAsync(email);
        Assert.True((await LoginAsync(email, ApiFixture.TestPassword)).IsSuccess);
    }

    [Fact]
    public async Task Login_ForDeactivatedUser_IsForbidden()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var email = await _api.GetEmailAsync(courier.UserId);

        (await admin.PutAsync<UserDetailsResponse>($"/api/users/{courier.UserId}", new UpdateUserRequest
        {
            FirstName = "Тест",
            LastName = "Курьер",
            Role = UserRole.Courier,
            IsActive = false
        })).Require();

        var response = await LoginAsync(email, ApiFixture.TestPassword);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_VerifyCode_ResetPassword_FullFlow()
    {
        var user = await _api.CreateUserAsync(UserRole.Shop);
        var email = await _api.GetEmailAsync(user.UserId);
        var oldSession = await _api.LoginAsync(user.Phone);

        var resetToken = await RequestResetTokenAsync(email);

        (await ResetPasswordAsync(resetToken, NewPassword)).Require();

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, ApiFixture.TestPassword)).StatusCode);
        Assert.True((await LoginAsync(email, NewPassword)).IsSuccess);

        var refresh = await _api.CreateAnonymousClient().PostAsync<AuthResponse>("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = oldSession.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var reuse = await ResetPasswordAsync(resetToken, "Another123");
        Assert.False(reuse.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_ReturnsSuccess_AndSendsNothing()
    {
        var email = NewEmail();

        (await _api.CreateAnonymousClient()
            .PostAsync<object>("/api/auth/forgot-password", new EmailRequest { Email = email })).Require();

        Assert.Equal(0, _api.Email.SentCount(email));
        Assert.Null(_api.Email.GetCode(email));
    }

    [Fact]
    public async Task VerifyResetCode_WithWrongCode_Fails()
    {
        var user = await _api.CreateUserAsync(UserRole.Shop);
        var email = await _api.GetEmailAsync(user.UserId);
        var anonymous = _api.CreateAnonymousClient();

        await _api.ClearAuthThrottlesAsync(email);
        (await anonymous.PostAsync<object>("/api/auth/forgot-password", new EmailRequest { Email = email })).Require();

        var wrong = WrongCode(_api.Email.GetCode(email)!);

        var response = await anonymous.PostAsync<ResetTokenResponse>("/api/auth/verify-reset-code",
            new VerifyResetCodeRequest { Email = email, Code = wrong });

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithUnknownToken_Fails()
    {
        var response = await ResetPasswordAsync(new string('x', 43), NewPassword);

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_DoesNotBurnTheToken()
    {
        var user = await _api.CreateUserAsync(UserRole.Shop);
        var email = await _api.GetEmailAsync(user.UserId);

        var resetToken = await RequestResetTokenAsync(email);

        var weak = await ResetPasswordAsync(resetToken, "onlyletters");
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        (await ResetPasswordAsync(resetToken, NewPassword)).Require();
        Assert.True((await LoginAsync(email, NewPassword)).IsSuccess);
    }

    [Fact]
    public async Task AdminCreatedUser_SetsPasswordThroughReset_AndCanLogIn()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var email = NewEmail();

        var created = (await admin.PostAsync<UserDetailsResponse>("/api/users", new CreateUserRequest
        {
            FirstName = "Фаррух",
            LastName = "Курьеров",
            Phone = NewPhone(),
            Email = email,
            Role = UserRole.Courier
        })).Require();

        Assert.Equal(email, created.Email);
        Assert.False(created.EmailConfirmed);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, Password)).StatusCode);

        var resetToken = await RequestResetTokenAsync(email);
        (await ResetPasswordAsync(resetToken, Password)).Require();

        var auth = (await LoginAsync(email, Password)).Require();
        Assert.Equal("Courier", auth.User.Role);
    }

    [Fact]
    public async Task AdminCreateUser_WithTakenEmail_ReturnsConflict()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var existing = await _api.CreateUserAsync(UserRole.Courier);
        var email = await _api.GetEmailAsync(existing.UserId);

        var response = await admin.PostAsync<UserDetailsResponse>("/api/users", new CreateUserRequest
        {
            FirstName = "Другой",
            LastName = "Курьер",
            Phone = NewPhone(),
            Email = email,
            Role = UserRole.Courier
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsNewTokens_AndEndsOtherSessions()
    {
        var user = await _api.CreateUserAsync(UserRole.Shop);
        var email = await _api.GetEmailAsync(user.UserId);
        var otherSession = await _api.LoginAsync(user.Phone);
        var current = await _api.LoginAsync(user.Phone);
        var client = _api.CreateClientWithToken(current.AccessToken);

        var changed = (await client.PostAsync<AuthResponse>("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = ApiFixture.TestPassword,
            NewPassword = NewPassword
        })).Require();

        Assert.False(string.IsNullOrEmpty(changed.RefreshToken));

        var anonymous = _api.CreateAnonymousClient();

        var oldRefresh = await anonymous.PostAsync<AuthResponse>("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = otherSession.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);

        var newRefresh = await anonymous.PostAsync<AuthResponse>("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = changed.RefreshToken });
        Assert.True(newRefresh.IsSuccess);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, ApiFixture.TestPassword)).StatusCode);
        Assert.True((await LoginAsync(email, NewPassword)).IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Fails()
    {
        var user = await _api.CreateUserAsync(UserRole.Shop);

        var response = await user.PostAsync<AuthResponse>("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = "WrongPass1",
            NewPassword = NewPassword
        });

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ToSamePasswordOrWeakOne_Fails()
    {
        var user = await _api.CreateUserAsync(UserRole.Shop);

        var same = await user.PostAsync<AuthResponse>("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = ApiFixture.TestPassword,
            NewPassword = ApiFixture.TestPassword
        });
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);

        var weak = await user.PostAsync<AuthResponse>("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = ApiFixture.TestPassword,
            NewPassword = "onlyletters"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthorization_Returns401()
    {
        var response = await _api.CreateAnonymousClient().PostAsync<AuthResponse>("/api/auth/change-password",
            new ChangePasswordRequest { CurrentPassword = "Whatever1", NewPassword = NewPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}