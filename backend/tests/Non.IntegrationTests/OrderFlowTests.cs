using System.Net;
using Non.Application.Common;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OrderFlowTests
{
    private readonly ApiFixture _api;

    public OrderFlowTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task DebtOrder_OverLimit_IsRejected_AndWithinLimit_Passes()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin, 5m);
        var shop = await _api.CreateShopAsync(admin, debtLimit: 1000m);

        var first = (await _api.PlaceOrderAsync(shop.Owner, productId, 100, PaymentType.Debt)).Require();
        Assert.Equal(500m, first.TotalAmount);

        var tooBig = await _api.PlaceOrderAsync(shop.Owner, productId, 160, PaymentType.Debt);
        Assert.False(tooBig.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);

        var withinLimit = await _api.PlaceOrderAsync(shop.Owner, productId, 80, PaymentType.Debt);
        Assert.True(withinLimit.IsSuccess, withinLimit.Error);
    }

    [Fact]
    public async Task CreateOrder_WithSameClientId_IsIdempotent()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);
        var id = Guid.NewGuid();

        var first = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Now, id: id)).Require();
        var second = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Now, id: id)).Require();

        Assert.Equal(id, first.Id);
        Assert.Equal(id, second.Id);

        var list = (await shop.Owner.GetAsync<PagedResult<OrderResponse>>("/api/orders")).Require();
        Assert.Equal(1, list.TotalCount);
    }

    [Fact]
    public async Task NowOrder_RequiresPaymentAmountOnDelivery_AndPaymentIsRecorded()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var courier = await _api.CreateUserAsync(UserRole.Courier);
        var productId = await _api.CreateProductAsync(admin, 5m);
        var shop = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 20, PaymentType.Now)).Require();
        await _api.AdvanceToOnTheWayAsync(admin, courier, order.Id);

        var withoutAmount = await courier.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest { Status = OrderStatus.Delivered });

        Assert.False(withoutAmount.IsSuccess);

        var stillOnTheWay = (await courier.GetAsync<OrderDetailsResponse>($"/api/orders/{order.Id}")).Require();
        Assert.Equal("OnTheWay", stillOnTheWay.Status);

        var delivered = (await courier.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest
            {
                Status = OrderStatus.Delivered,
                PaymentAmount = 100m,
                PaymentMethod = PaymentMethod.Cash
            })).Require();

        Assert.Equal("Delivered", delivered.Status);

        var balance = await _api.GetBalanceAsync(admin, shop.ShopId);
        Assert.Equal(100m, balance.DeliveredTotal);
        Assert.Equal(100m, balance.PaidTotal);
        Assert.Equal(0m, balance.Debt);
    }

    [Fact]
    public async Task Courier_CannotChangeForeignOrder()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var owner = await _api.CreateUserAsync(UserRole.Courier);
        var stranger = await _api.CreateUserAsync(UserRole.Courier);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt)).Require();

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/courier",
            new AssignCourierRequest { CourierId = owner.UserId })).Require();

        var response = await stranger.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest { Status = OrderStatus.OnTheWay });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Shop_CannotSeeForeignOrder()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shopA = await _api.CreateShopAsync(admin);
        var shopB = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shopA.Owner, productId, 10, PaymentType.Debt)).Require();

        var response = await shopB.Owner.GetAsync<OrderDetailsResponse>($"/api/orders/{order.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Shop_CanCancelNewOrder_ButNotTwice()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt)).Require();

        var cancelled = (await shop.Owner.PostAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/cancel")).Require();
        Assert.Equal("Cancelled", cancelled.Status);

        var again = await shop.Owner.PostAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/cancel");
        Assert.False(again.IsSuccess);
    }
}