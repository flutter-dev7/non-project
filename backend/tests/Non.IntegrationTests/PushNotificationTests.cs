using System.Net;
using Non.Application.DTOs.Devices.Request;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PushNotificationTests
{
    private readonly ApiFixture _api;

    public PushNotificationTests(ApiFixture api)
    {
        _api = api;
    }

    private static string NewToken(string prefix = "test-") =>
        prefix + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private Task<Non.Application.Common.PushMessage> WaitFor(string token, Guid orderId, string type) =>
        _api.Push.WaitForAsync(m =>
            m.Token == token &&
            m.Data.GetValueOrDefault("orderId") == orderId.ToString() &&
            m.Data.GetValueOrDefault("type") == type);

    [Fact]
    public async Task OrderLifecycle_NotifiesTheRightPeople()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        var adminToken = NewToken();
        var courierToken = NewToken();
        var shopToken = NewToken();

        await _api.RegisterFcmTokenAsync(admin, adminToken);
        await _api.RegisterFcmTokenAsync(courier, courierToken);
        await _api.RegisterFcmTokenAsync(shop.Owner, shopToken);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt)).Require();

        var created = await WaitFor(adminToken, order.Id, "order_created");
        Assert.Equal("Новый заказ", created.Title);

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/courier",
            new AssignCourierRequest { CourierId = courier.UserId })).Require();

        var assigned = await WaitFor(courierToken, order.Id, "order_assigned");
        Assert.Equal("Вам назначен заказ", assigned.Title);

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest { Status = OrderStatus.Accepted })).Require();

        var accepted = await WaitFor(shopToken, order.Id, "order_status");
        Assert.Equal("Accepted", accepted.Data["status"]);
        Assert.Equal("Заказ принят", accepted.Title);

        Assert.DoesNotContain(_api.Push.Sent, m =>
            m.Token == shopToken && m.Data.GetValueOrDefault("type") == "order_created");
    }

    [Fact]
    public async Task OrderCreatedByAdmin_NotifiesShopOwner_ButNotTheAdmin()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        var adminToken = NewToken();
        var shopToken = NewToken();

        await _api.RegisterFcmTokenAsync(admin, adminToken);
        await _api.RegisterFcmTokenAsync(shop.Owner, shopToken);

        var order = (await admin.PostAsync<OrderDetailsResponse>("/api/orders", new CreateOrderRequest
        {
            ShopId = shop.ShopId,
            DeliveryDate = Dates.Ahead(5),
            PaymentType = PaymentType.Debt,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 10 } }
        })).Require();

        await WaitFor(shopToken, order.Id, "order_created");

        Assert.DoesNotContain(_api.Push.Sent, m =>
            m.Token == adminToken && m.Data.GetValueOrDefault("orderId") == order.Id.ToString());
    }

    [Fact]
    public async Task ShopCancelsOrder_NotifiesAdminAndCourier()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        var adminToken = NewToken();
        var courierToken = NewToken();

        await _api.RegisterFcmTokenAsync(admin, adminToken);
        await _api.RegisterFcmTokenAsync(courier, courierToken);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt)).Require();

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/courier",
            new AssignCourierRequest { CourierId = courier.UserId })).Require();

        (await shop.Owner.PostAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/cancel")).Require();

        await WaitFor(adminToken, order.Id, "order_cancelled");
        await WaitFor(courierToken, order.Id, "order_cancelled");
    }

    [Fact]
    public async Task Token_MovesToNewUser_AndCanBeCleared()
    {
        var first = await _api.CreateUserAsync(UserRole.Courier);
        var second = await _api.CreateUserAsync(UserRole.Courier);
        var token = NewToken();

        await _api.RegisterFcmTokenAsync(first, token);
        Assert.Equal(token, await _api.GetFcmTokenAsync(first.UserId));

        await _api.RegisterFcmTokenAsync(second, token);
        Assert.Null(await _api.GetFcmTokenAsync(first.UserId));
        Assert.Equal(token, await _api.GetFcmTokenAsync(second.UserId));

        (await second.DeleteAsync<object>("/api/devices/fcm-token")).Require();
        Assert.Null(await _api.GetFcmTokenAsync(second.UserId));
    }

    [Fact]
    public async Task InvalidToken_IsClearedAfterFailedDelivery()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        await _api.RegisterFcmTokenAsync(courier, NewToken("invalid-"));

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt)).Require();

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/courier",
            new AssignCourierRequest { CourierId = courier.UserId })).Require();

        var deadline = DateTime.UtcNow.AddSeconds(5);
        string? token;

        do
        {
            await Task.Delay(100);
            token = await _api.GetFcmTokenAsync(courier.UserId);
        }
        while (token is not null && DateTime.UtcNow < deadline);

        Assert.Null(token);
    }

    [Fact]
    public async Task RegisterToken_WithTooShortValue_ReturnsValidationError()
    {
        var courier = await _api.CreateUserAsync(UserRole.Courier);

        var response = await courier.PutAsync<object>("/api/devices/fcm-token",
            new RegisterFcmTokenRequest { Token = "short" });

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisterToken_WithoutAuthorization_Returns401()
    {
        var response = await _api.CreateAnonymousClient().PutAsync<object>("/api/devices/fcm-token",
            new RegisterFcmTokenRequest { Token = NewToken() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}