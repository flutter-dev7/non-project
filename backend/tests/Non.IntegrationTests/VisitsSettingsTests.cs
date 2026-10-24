using System.Net;
using Non.Application.Common;
using Non.Application.DTOs.Settings.Request;
using Non.Application.DTOs.Settings.Response;
using Non.Application.DTOs.Visits.Request;
using Non.Application.DTOs.Visits.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class VisitsSettingsTests
{
    private readonly ApiFixture _api;

    public VisitsSettingsTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task Agent_CreatesVisit_AndSeesOnlyOwnVisits()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var agent = await _api.CreateUserAsync(UserRole.Agent);
        var otherAgent = await _api.CreateUserAsync(UserRole.Agent);
        var shop = await _api.CreateShopAsync(admin);

        var visit = (await agent.PostAsync<VisitResponse>("/api/visits",
            new CreateVisitRequest { ShopId = shop.ShopId, Note = "Нужно больше лепёшек" })).Require();

        Assert.Equal(agent.UserId, visit.AgentId);

        var own = (await agent.GetAsync<PagedResult<VisitResponse>>("/api/visits")).Require();
        Assert.Contains(own.Items, v => v.Id == visit.Id);
        Assert.All(own.Items, v => Assert.Equal(agent.UserId, v.AgentId));

        var foreign = (await otherAgent.GetAsync<PagedResult<VisitResponse>>("/api/visits")).Require();
        Assert.DoesNotContain(foreign.Items, v => v.Id == visit.Id);

        var foreignDetails = await otherAgent.GetAsync<VisitResponse>($"/api/visits/{visit.Id}");
        Assert.Equal(HttpStatusCode.NotFound, foreignDetails.StatusCode);

        var all = (await admin.GetAsync<PagedResult<VisitResponse>>($"/api/visits?shopId={shop.ShopId}")).Require();
        Assert.Contains(all.Items, v => v.Id == visit.Id);
    }

    [Fact]
    public async Task Visit_ToUnknownShop_ReturnsNotFound()
    {
        var agent = await _api.CreateUserAsync(UserRole.Agent);

        var response = await agent.PostAsync<VisitResponse>("/api/visits",
            new CreateVisitRequest { ShopId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Shop_CannotUseVisits()
    {
        var shop = await _api.CreateUserAsync(UserRole.Shop);

        var response = await shop.GetAsync<PagedResult<VisitResponse>>("/api/visits");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OrderCutoffSetting_ChangesBehaviorImmediately()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        try
        {
            (await admin.PutAsync<SettingsResponse>("/api/settings",
                new UpdateSettingsRequest { OrderCutoffTime = "00:00" })).Require();

            var rejected = await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt, daysAhead: 1);
            Assert.False(rejected.IsSuccess);

            (await admin.PutAsync<SettingsResponse>("/api/settings",
                new UpdateSettingsRequest { OrderCutoffTime = "23:59" })).Require();

            var accepted = await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt, daysAhead: 1);
            Assert.True(accepted.IsSuccess, accepted.Error);
        }
        finally
        {
            await admin.PutAsync<SettingsResponse>("/api/settings",
                new UpdateSettingsRequest { OrderCutoffTime = "18:00" });
        }
    }

    [Fact]
    public async Task Settings_RejectInvalidTime_AndAreAdminOnly()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);

        var invalid = await admin.PutAsync<SettingsResponse>("/api/settings",
            new UpdateSettingsRequest { OrderCutoffTime = "25:00" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var forbidden = await courier.GetAsync<SettingsResponse>("/api/settings");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }
}