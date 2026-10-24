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
public class AuthSecurityTests
{
    private readonly ApiFixture _api;

    public AuthSecurityTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task Login_WithSmsCode_ReturnsTokenAndCurrentUser()
    {
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var anonymous = _api.CreateAnonymousClient();

        (await anonymous.PostAsync<object>("/api/auth/send-code", new SendCodeRequest { Phone = courier.Phone })).Require();

        var code = _api.Sms.GetCode(courier.Phone);
        Assert.NotNull(code);

        var auth = (await anonymous.PostAsync<AuthResponse>("/api/auth/verify",
            new VerifyCodeRequest { Phone = courier.Phone, Code = code! })).Require();

        Assert.False(string.IsNullOrEmpty(auth.AccessToken));

        var authorized = _api.CreateClientWithToken(auth.AccessToken);
        var me = (await authorized.GetAsync<UserResponse>("/api/auth/me")).Require();

        Assert.Equal("Courier", me.Role);
        Assert.Equal(courier.UserId, me.Id);
    }

    [Fact]
    public async Task Login_WithWrongCode_Fails()
    {
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var anonymous = _api.CreateAnonymousClient();

        (await anonymous.PostAsync<object>("/api/auth/send-code", new SendCodeRequest { Phone = courier.Phone })).Require();

        var actual = _api.Sms.GetCode(courier.Phone);
        var wrong = actual == "000000" ? "111111" : "000000";

        var response = await anonymous.PostAsync<AuthResponse>("/api/auth/verify",
            new VerifyCodeRequest { Phone = courier.Phone, Code = wrong });

        Assert.False(response.IsSuccess);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_NewUser_BecomesShopRoleAfterVerification()
    {
        var phone = "+9929" + Random.Shared.Next(0, 100_000_000).ToString("D8");
        var anonymous = _api.CreateAnonymousClient();

        (await anonymous.PostAsync<object>("/api/auth/register", new RegisterRequest
        {
            FirstName = "Новый",
            LastName = "Клиент",
            Phone = phone
        })).Require();

        if (_api.Sms.GetCode(phone) is null)
            (await anonymous.PostAsync<object>("/api/auth/send-code", new SendCodeRequest { Phone = phone })).Require();

        var code = _api.Sms.GetCode(phone);
        Assert.NotNull(code);

        var auth = (await anonymous.PostAsync<AuthResponse>("/api/auth/verify",
            new VerifyCodeRequest { Phone = phone, Code = code! })).Require();

        Assert.Equal("Shop", auth.User.Role);
        Assert.Equal("Новый Клиент", auth.User.FullName);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401WithResultBody()
    {
        var response = await _api.CreateAnonymousClient().GetAsync<object>("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.IsSuccess);
        Assert.False(string.IsNullOrEmpty(response.Error));
    }

    [Fact]
    public async Task Roles_AreEnforced_With403ResultBody()
    {
        var shop = await _api.CreateUserAsync(UserRole.Shop);
        var courier = await _api.CreateUserAsync(UserRole.Courier);

        var reports = await shop.GetAsync<object>("/api/reports/production");
        var users = await courier.GetAsync<object>("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, reports.StatusCode);
        Assert.False(reports.IsSuccess);
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
    }

    [Fact]
    public async Task DeactivatedUser_LosesAccessWithExistingToken()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);

        Assert.Equal(HttpStatusCode.OK, (await courier.GetAsync<UserResponse>("/api/auth/me")).StatusCode);

        (await admin.PutAsync<UserDetailsResponse>($"/api/users/{courier.UserId}", new UpdateUserRequest
        {
            FirstName = "Тест",
            LastName = "Курьер",
            Role = UserRole.Courier,
            IsActive = false
        })).Require();

        await _api.ClearUserAccessCacheAsync(courier.UserId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await courier.GetAsync<UserResponse>("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task ChangedRole_InvalidatesOldToken()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);

        Assert.Equal(HttpStatusCode.OK, (await courier.GetAsync<UserResponse>("/api/auth/me")).StatusCode);

        (await admin.PutAsync<UserDetailsResponse>($"/api/users/{courier.UserId}", new UpdateUserRequest
        {
            FirstName = "Тест",
            LastName = "Курьер",
            Role = UserRole.Agent,
            IsActive = true
        })).Require();

        await _api.ClearUserAccessCacheAsync(courier.UserId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await courier.GetAsync<UserResponse>("/api/auth/me")).StatusCode);
    }
}