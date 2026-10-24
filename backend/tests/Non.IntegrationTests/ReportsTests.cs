using Non.Application.Common;
using Non.Application.DTOs.Debts.Response;
using Non.Application.DTOs.Reports.Response;
using Non.Application.DTOs.Shops.Request;
using Non.Application.DTOs.Shops.Response;
using Non.Domain.Enums;
using Non.IntegrationTests.Infrastructure;
using Xunit;

namespace Non.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ReportsTests
{
    private readonly ApiFixture _api;

    public ReportsTests(ApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task ProductionReport_SumsOrdersForDate_AndSkipsCancelled()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin);
        var shopA = await _api.CreateShopAsync(admin);
        var shopB = await _api.CreateShopAsync(admin);

        (await _api.PlaceOrderAsync(shopA.Owner, productId, 100, PaymentType.Debt, daysAhead: 12)).Require();
        (await _api.PlaceOrderAsync(shopB.Owner, productId, 20, PaymentType.Now, daysAhead: 12)).Require();

        var cancelled = (await _api.PlaceOrderAsync(shopB.Owner, productId, 50, PaymentType.Now, daysAhead: 12)).Require();
        (await shopB.Owner.PostAsync<object>($"/api/orders/{cancelled.Id}/cancel")).Require();

        var date = Dates.Ahead(12).ToString("yyyy-MM-dd");
        var report = (await admin.GetAsync<ProductionReportResponse>($"/api/reports/production?date={date}")).Require();

        var row = Assert.Single(report.Items, i => i.ProductId == productId);
        Assert.Equal(120, row.Quantity);
        Assert.True(report.OrdersCount >= 2);
    }

    [Fact]
    public async Task SalesReport_CountsOnlyDeliveredOrders_AndReceivedPayments()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin, 5m);
        var shopA = await _api.CreateShopAsync(admin);
        var shopB = await _api.CreateShopAsync(admin);

        var delivered = (await _api.PlaceOrderAsync(shopA.Owner, productId, 100, PaymentType.Debt, daysAhead: 14)).Require();
        var paidNow = (await _api.PlaceOrderAsync(shopB.Owner, productId, 20, PaymentType.Now, daysAhead: 14)).Require();
        (await _api.PlaceOrderAsync(shopB.Owner, productId, 40, PaymentType.Debt, daysAhead: 14)).Require();

        await _api.DeliverAsync(admin, delivered.Id);
        await _api.DeliverAsync(admin, paidNow.Id, paymentAmount: 100m);

        var from = Dates.Today.ToString("yyyy-MM-dd");
        var to = Dates.Ahead(14).ToString("yyyy-MM-dd");
        var report = (await admin.GetAsync<SalesReportResponse>($"/api/reports/sales?from={from}&to={to}")).Require();

        var row = Assert.Single(report.ByProduct, p => p.ProductId == productId);
        Assert.Equal(120, row.Quantity);
        Assert.Equal(600m, row.Amount);
        Assert.True(report.ReceivedPayments >= 100m);
    }

    [Fact]
    public async Task SalesReport_WithInvalidPeriod_Fails()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);

        var response = await admin.GetAsync<SalesReportResponse>("/api/reports/sales?from=2026-10-10&to=2026-10-01");

        Assert.False(response.IsSuccess);
    }

    [Fact]
    public async Task DebtsList_ShowsShopsWithDebt_AndOverLimitFlag()
    {
        var admin = await _api.CreateUserAsync(UserRole.Admin);
        var productId = await _api.CreateProductAsync(admin, 5m);
        var shop = await _api.CreateShopAsync(admin, debtLimit: 1000m);

        var order = (await _api.PlaceOrderAsync(shop.Owner, productId, 100, PaymentType.Debt)).Require();
        await _api.DeliverAsync(admin, order.Id);

        var before = (await admin.GetAsync<PagedResult<DebtListItemResponse>>("/api/debts?pageSize=100")).Require();
        var itemBefore = Assert.Single(before.Items, i => i.ShopId == shop.ShopId);
        Assert.Equal(500m, itemBefore.Debt);
        Assert.False(itemBefore.IsOverLimit);

        (await admin.PutAsync<ShopResponse>($"/api/shops/{shop.ShopId}/debt-limit",
            new SetDebtLimitRequest { DebtLimit = 300m })).Require();

        var after = (await admin.GetAsync<PagedResult<DebtListItemResponse>>("/api/debts?pageSize=100")).Require();
        var itemAfter = Assert.Single(after.Items, i => i.ShopId == shop.ShopId);
        Assert.True(itemAfter.IsOverLimit);
    }
}