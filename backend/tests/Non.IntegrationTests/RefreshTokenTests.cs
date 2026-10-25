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
public class RefreshTokenTests
{
    private readonly ApiFixture _api;

    public RefreshTokenTests(ApiFixture api)
    {
        _api = api;
    }

    private Task<ApiResponse<AuthResponse>> RefreshAsync(string refreshToken) =>
        _api.CreateAnonymousClient().PostAsync<AuthResponse>("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });

    [Fact]
    public async Task Login_ReturnsRefreshToken_AndRefreshRotatesIt()
    {
        var user = await _api.CreateUserAsync(UserRole.Courier);
        var auth = await _api.LoginAsync(user.Phone);

        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));

        var refreshed = (await RefreshAsync(auth.RefreshToken)).Require();

        Assert.NotEqual(auth.RefreshToken, refreshed.RefreshToken);
        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));

        var me = await _api.CreateClientWithToken(refreshed.AccessToken).GetAsync<UserResponse>("/api/auth/me");
        Assert.True(me.IsSuccess);
        Assert.Equal(user.UserId, me.Data!.Id);
    }

    [Fact]
    public async Task ReusingRotatedToken_RevokesWholeSession()
    {
        var user = await _api.CreateUserAsync(UserRole.Courier);
        var auth = await _api.LoginAsync(user.Phone);

        var refreshed = (await RefreshAsync(auth.RefreshToken)).Require();

        var reuse = await RefreshAsync(auth.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var newTokenAfterTheft = await RefreshAsync(refreshed.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, newTokenAfterTheft.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesToken_AndIsIdempotent()
    {
        var user = await _api.CreateUserAsync(UserRole.Courier);
        var auth = await _api.LoginAsync(user.Phone);
        var anonymous = _api.CreateAnonymousClient();

        (await anonymous.PostAsync<object>("/api/auth/logout",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken })).Require();

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(auth.RefreshToken)).StatusCode);

        var again = await anonymous.PostAsync<object>("/api/auth/logout",
            new RefreshTokenRequest { RefreshToken = auth.RefreshToken });
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var response = await RefreshAsync(new string('a', 86));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public async Task Refresh_ForDeactivatedUser_Fails()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var auth = await _api.LoginAsync(courier.Phone);

        (await admin.PutAsync<UserDetailsResponse>($"/api/users/{courier.UserId}", new UpdateUserRequest
        {
            FirstName = "Тест",
            LastName = "Курьер",
            Role = UserRole.Courier,
            IsActive = false
        })).Require();

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(auth.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task LogoutAll_RevokesEverySession()
    {
        var user = await _api.CreateUserAsync(UserRole.Courier);
        var first = await _api.LoginAsync(user.Phone);
        var second = await _api.LoginAsync(user.Phone);

        var authorized = _api.CreateClientWithToken(second.AccessToken);
        (await authorized.PostAsync<object>("/api/auth/logout-all")).Require();

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(first.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_WithMalformedBody_ReturnsValidationError()
    {
        var response = await RefreshAsync("short");

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}