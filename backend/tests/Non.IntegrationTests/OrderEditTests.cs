using System.Net;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;
using Non.Application.DTOs.Settings.Request;
using Non.Application.DTOs.Settings.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OrderEditTests
{
    private readonly ApiFixture _api;

    public OrderEditTests(ApiFixture api)
    {
        _api = api;
    }

    private static UpdateOrderRequest Edit(Guid productId, int quantity, PaymentType type, int daysAhead = 6) => new()
    {
        DeliveryDate = Dates.Ahead(daysAhead),
        PaymentType = type,
        Items = new List<CreateOrderItemRequest>
        {
            new() { ProductId = productId, Quantity = quantity }
        }
    };

    [Fact]
    public async Task Shop_CanEditNewAndAcceptedOrder_ButNotAfterBakingStarts()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productA = await _api.CreateProductAsync(admin, 5m);
        var productB = await _api.CreateProductAsync(admin, 8m);
        var shop = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productA, 100, PaymentType.Debt)).Require();

        var updated = (await shop.Owner.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}", new UpdateOrderRequest
        {
            DeliveryDate = Dates.Ahead(6),
            PaymentType = PaymentType.Now,
            Note = "Изменено",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = productA, Quantity = 40 },
                new() { ProductId = productB, Quantity = 10 }
            }
        })).Require();

        Assert.Equal(280m, updated.TotalAmount);
        Assert.Equal(2, updated.Items.Count);
        Assert.Equal("Now", updated.PaymentType);
        Assert.Equal(Dates.Ahead(6), updated.DeliveryDate);
        Assert.Equal("Изменено", updated.Note);

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest { Status = OrderStatus.Accepted })).Require();

        var acceptedEdit = (await shop.Owner.PutAsync<OrderDetailsResponse>(
            $"/api/orders/{order.Id}", Edit(productA, 10, PaymentType.Now))).Require();
        Assert.Equal(50m, acceptedEdit.TotalAmount);
        Assert.Single(acceptedEdit.Items);

        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest { Status = OrderStatus.Baking })).Require();

        var bakingEdit = await shop.Owner.PutAsync<OrderDetailsResponse>(
            $"/api/orders/{order.Id}", Edit(productA, 20, PaymentType.Now));

        Assert.False(bakingEdit.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, bakingEdit.StatusCode);
    }

    [Fact]
    public async Task Edit_RechecksDebtLimit_WithoutCountingOwnAmountTwice()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin, 5m);
        var shop = await _api.CreateShopAsync(admin, debtLimit: 1000m);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 100, PaymentType.Debt)).Require();

        var tooBig = await shop.Owner.PutAsync<OrderDetailsResponse>(
            $"/api/orders/{order.Id}", Edit(productId, 240, PaymentType.Debt));
        Assert.False(tooBig.IsSuccess);

        var within = await shop.Owner.PutAsync<OrderDetailsResponse>(
            $"/api/orders/{order.Id}", Edit(productId, 180, PaymentType.Debt));
        Assert.True(within.IsSuccess, within.Error);
        Assert.Equal(900m, within.Data!.TotalAmount);
    }

    [Fact]
    public async Task Edit_ForeignShopOrder_IsForbidden()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shopA = await _api.CreateShopAsync(admin);
        var shopB = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shopA.Owner, productId, 10, PaymentType.Debt)).Require();

        var response = await shopB.Owner.PutAsync<OrderDetailsResponse>(
            $"/api/orders/{order.Id}", Edit(productId, 20, PaymentType.Debt));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Edit_UnknownOrder_ReturnsNotFound()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);

        var response = await admin.PutAsync<OrderDetailsResponse>(
            $"/api/orders/{Guid.NewGuid()}", Edit(productId, 10, PaymentType.Debt));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TomorrowOrder_AfterCutoff_CannotBeEditedOrCancelledByShop_ButAdminCan()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shop = await _api.CreateShopAsync(admin);

        try
        {
            (await admin.PutAsync<SettingsResponse>("/api/settings",
                new UpdateSettingsRequest { OrderCutoffTime = "23:59" })).Require();

            var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 10, PaymentType.Debt, daysAhead: 1)).Require();

            (await admin.PutAsync<SettingsResponse>("/api/settings",
                new UpdateSettingsRequest { OrderCutoffTime = "00:00" })).Require();

            var edit = await shop.Owner.PutAsync<OrderDetailsResponse>(
                $"/api/orders/{order.Id}", Edit(productId, 20, PaymentType.Debt));
            Assert.False(edit.IsSuccess);

            var cancel = await shop.Owner.PostAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/cancel");
            Assert.False(cancel.IsSuccess);

            var adminCancel = (await admin.PostAsync<OrderDetailsResponse>($"/api/orders/{order.Id}/cancel")).Require();
            Assert.Equal("Cancelled", adminCancel.Status);
        }
        finally
        {
            await admin.PutAsync<SettingsResponse>("/api/settings",
                new UpdateSettingsRequest { OrderCutoffTime = "18:00" });
        }
    }
}