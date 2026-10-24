using System.Net;
using Non.Application.DTOs.Payments.Request;
using Non.Application.DTOs.Payments.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class PaymentsTests
{
    private readonly ApiFixture _api;

    public PaymentsTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<(ApiClient Admin, ShopContext Shop)> ShopWithDeliveredDebtAsync(int quantity = 100)
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin, 5m);
        var shop = await _api.CreateShopAsync(admin);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, quantity, PaymentType.Debt)).Require();
        await _api.DeliverAsync(admin, order.Id);

        return (admin, shop);
    }

    [Fact]
    public async Task DeliveredDebtOrder_AppearsInBalance_AndPaymentsReduceDebt()
    {
        var (admin, shop) = await ShopWithDeliveredDebtAsync();

        var balance = await _api.GetBalanceAsync(admin, shop.ShopId);
        Assert.Equal(500m, balance.Debt);
        Assert.Single(balance.UnpaidOrders);

        (await _api.PayAsync(admin, shop.ShopId, 300m)).Require();

        balance = await _api.GetBalanceAsync(admin, shop.ShopId);
        Assert.Equal(200m, balance.Debt);
        Assert.Equal(200m, balance.UnpaidOrders.Single().RemainingAmount);
    }

    [Fact]
    public async Task Payment_GreaterThanDebt_IsRejected()
    {
        var (admin, shop) = await ShopWithDeliveredDebtAsync();

        var response = await _api.PayAsync(admin, shop.ShopId, 600m);

        Assert.False(response.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(500m, (await _api.GetBalanceAsync(admin, shop.ShopId)).Debt);
    }

    [Fact]
    public async Task Reversal_RestoresDebt_AndCannotBeRepeated()
    {
        var (admin, shop) = await ShopWithDeliveredDebtAsync();

        var payment = (await _api.PayAsync(admin, shop.ShopId, 300m)).Require();

        var reversal = (await admin.PostAsync<PaymentResponse>($"/api/payments/{payment.Id}/reverse",
            new ReversePaymentRequest { Reason = "Ошибка ввода" })).Require();

        Assert.True(reversal.IsReversal);
        Assert.Equal(-300m, reversal.Amount);
        Assert.Equal(500m, (await _api.GetBalanceAsync(admin, shop.ShopId)).Debt);

        var again = await admin.PostAsync<PaymentResponse>($"/api/payments/{payment.Id}/reverse",
            new ReversePaymentRequest { Reason = "Ещё раз" });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(500m, (await _api.GetBalanceAsync(admin, shop.ShopId)).Debt);
    }

    [Fact]
    public async Task Reversal_ByNonAdmin_IsForbidden()
    {
        var (admin, shop) = await ShopWithDeliveredDebtAsync();
        var courier = await _api.CreateUserAsync(UserRole.Courier);

        var payment = (await _api.PayAsync(admin, shop.ShopId, 100m)).Require();

        var response = await courier.PostAsync<PaymentResponse>($"/api/payments/{payment.Id}/reverse",
            new ReversePaymentRequest { Reason = "Попытка" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Shop_CannotCreatePayments()
    {
        var (_, shop) = await ShopWithDeliveredDebtAsync();

        var response = await _api.PayAsync(shop.Owner, shop.ShopId, 100m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentPayments_CannotExceedDebt()
    {
        var (admin, shop) = await ShopWithDeliveredDebtAsync();

        var results = await Task.WhenAll(
            _api.PayAsync(admin, shop.ShopId, 400m),
            _api.PayAsync(admin, shop.ShopId, 400m));

        Assert.Equal(1, results.Count(r => r.IsSuccess));

        var balance = await _api.GetBalanceAsync(admin, shop.ShopId);
        Assert.Equal(100m, balance.Debt);
    }

    [Fact]
    public async Task ShopSeesOwnBalanceAndPayments()
    {
        var (admin, shop) = await ShopWithDeliveredDebtAsync();

        (await _api.PayAsync(admin, shop.ShopId, 100m)).Require();

        var balance = (await shop.Owner.GetAsync<Non.Application.DTOs.Debts.Response.ShopBalanceResponse>(
            "/api/shops/my/balance")).Require();

        Assert.Equal(400m, balance.Debt);

        var payments = (await shop.Owner.GetAsync<Non.Application.Common.PagedResult<PaymentResponse>>(
            "/api/payments")).Require();

        Assert.Single(payments.Items);
    }
}