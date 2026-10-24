using Non.Application.Common;
using Xunit;

namespace Non.UnitTests.Application;

public class DebtCalculatorTests
{
    private static readonly DateTime T0 = new(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc);

    private static DeliveredOrderInfo Order(string date, decimal total, int createdMinutes = 0, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), DateOnly.Parse(date), T0.AddMinutes(createdMinutes), total);

    [Fact]
    public void Allocate_PaymentCoversOldestOrderFirst()
    {
        var older = Order("2026-10-26", 500m);
        var newer = Order("2026-10-27", 400m);

        var result = DebtCalculator.Allocate(new[] { newer, older }, 600m);

        Assert.Equal(older.Id, result[0].OrderId);
        Assert.Equal(500m, result[0].PaidAmount);
        Assert.Equal(0m, result[0].RemainingAmount);
        Assert.Equal(newer.Id, result[1].OrderId);
        Assert.Equal(100m, result[1].PaidAmount);
        Assert.Equal(300m, result[1].RemainingAmount);
    }

    [Fact]
    public void Allocate_SameDeliveryDate_OrdersByCreatedAt()
    {
        var first = Order("2026-10-26", 100m, createdMinutes: 0);
        var second = Order("2026-10-26", 100m, createdMinutes: 5);

        var result = DebtCalculator.Allocate(new[] { second, first }, 100m);

        Assert.Equal(first.Id, result[0].OrderId);
        Assert.Equal(100m, result[0].PaidAmount);
        Assert.Equal(0m, result[1].PaidAmount);
    }

    [Fact]
    public void Allocate_NothingPaid_AllOrdersUnpaid()
    {
        var result = DebtCalculator.Allocate(new[] { Order("2026-10-26", 500m), Order("2026-10-27", 400m) }, 0m);

        Assert.All(result, r => Assert.Equal(0m, r.PaidAmount));
        Assert.Equal(900m, result.Sum(r => r.RemainingAmount));
    }

    [Fact]
    public void Allocate_NegativePaidTotal_IsTreatedAsZero()
    {
        var result = DebtCalculator.Allocate(new[] { Order("2026-10-26", 500m) }, -50m);

        Assert.Equal(0m, result[0].PaidAmount);
    }

    [Fact]
    public void Allocate_PaidMoreThanTotal_DoesNotOverAllocate()
    {
        var result = DebtCalculator.Allocate(new[] { Order("2026-10-26", 500m), Order("2026-10-27", 400m) }, 5000m);

        Assert.Equal(900m, result.Sum(r => r.PaidAmount));
        Assert.All(result, r => Assert.Equal(0m, r.RemainingAmount));
    }

    [Fact]
    public void Allocate_NoOrders_ReturnsEmpty()
    {
        Assert.Empty(DebtCalculator.Allocate(Array.Empty<DeliveredOrderInfo>(), 100m));
    }

    [Fact]
    public void ShopBalance_DebtIsDeliveredMinusPaid()
    {
        var balance = new ShopBalance(1000m, 300m, 250m);

        Assert.Equal(700m, balance.Debt);
    }
}