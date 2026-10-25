using Non.Domain.Common;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Xunit;

namespace Non.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Order CreateOrder() => new()
    {
        ShopId = Guid.NewGuid(),
        CreatedByUserId = UserId,
        DeliveryDate = new DateOnly(2026, 10, 30),
        PaymentType = PaymentType.Debt
    };

    [Fact]
    public void NewOrder_StartsWithNewStatus()
    {
        Assert.Equal(OrderStatus.New, CreateOrder().Status);
    }

    [Fact]
    public void AddItem_CalculatesTotal()
    {
        var order = CreateOrder();

        order.AddItem(Guid.NewGuid(), 100, 5m);
        order.AddItem(Guid.NewGuid(), 50, 8m);

        Assert.Equal(900m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public void AddItem_SameProductTwice_MergesQuantity()
    {
        var order = CreateOrder();
        var productId = Guid.NewGuid();

        order.AddItem(productId, 10, 5m);
        order.AddItem(productId, 15, 5m);

        Assert.Single(order.Items);
        Assert.Equal(25, order.Items.First().Quantity);
        Assert.Equal(125m, order.TotalAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddItem_WithNonPositiveQuantity_Throws(int quantity)
    {
        Assert.Throws<DomainException>(() => CreateOrder().AddItem(Guid.NewGuid(), quantity, 5m));
    }

    [Fact]
    public void AddItem_WithNegativePrice_Throws()
    {
        Assert.Throws<DomainException>(() => CreateOrder().AddItem(Guid.NewGuid(), 1, -1m));
    }

    [Fact]
    public void ChangeStatus_FollowsFullChainAndRecordsHistory()
    {
        var order = CreateOrder();

        order.ChangeStatus(OrderStatus.Accepted, UserId);
        order.ChangeStatus(OrderStatus.Baking, UserId);
        order.ChangeStatus(OrderStatus.OnTheWay, UserId);
        order.ChangeStatus(OrderStatus.Delivered, UserId);

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(4, order.StatusHistory.Count);
    }

    [Theory]
    [InlineData(OrderStatus.New, OrderStatus.Delivered)]
    [InlineData(OrderStatus.New, OrderStatus.Baking)]
    [InlineData(OrderStatus.Accepted, OrderStatus.OnTheWay)]
    [InlineData(OrderStatus.Baking, OrderStatus.Accepted)]
    [InlineData(OrderStatus.Delivered, OrderStatus.OnTheWay)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Accepted)]
    public void ChangeStatus_InvalidTransition_Throws(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrder();
        order.Status = from;

        Assert.Throws<DomainException>(() => order.ChangeStatus(to, UserId));
        Assert.Equal(from, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.New, true)]
    [InlineData(OrderStatus.Accepted, true)]
    [InlineData(OrderStatus.Baking, false)]
    [InlineData(OrderStatus.OnTheWay, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void CanBeCancelled_DependsOnStatus(OrderStatus status, bool expected)
    {
        var order = CreateOrder();
        order.Status = status;

        Assert.Equal(expected, order.CanBeCancelled);
    }

    [Fact]
    public void Cancel_NewOrder_SetsCancelledAndRecordsHistory()
    {
        var order = CreateOrder();

        order.Cancel(UserId);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Single(order.StatusHistory);
    }

    [Fact]
    public void Cancel_AfterBakingStarted_Throws()
    {
        var order = CreateOrder();
        order.Status = OrderStatus.Baking;

        Assert.Throws<DomainException>(() => order.Cancel(UserId));
        Assert.Equal(OrderStatus.Baking, order.Status);
    }
    
    [Theory]
    [InlineData(OrderStatus.New, true)]
    [InlineData(OrderStatus.Accepted, true)]
    [InlineData(OrderStatus.Baking, false)]
    [InlineData(OrderStatus.OnTheWay, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void CanBeEdited_DependsOnStatus(OrderStatus status, bool expected)
    {
        var order = CreateOrder();
        order.Status = status;

        Assert.Equal(expected, order.CanBeEdited);
    }

    [Fact]
    public void ReplaceItems_UpdatesExisting_AddsNew_RemovesMissing_AndRecalculates()
    {
        var order = CreateOrder();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        order.AddItem(a, 10, 5m);
        order.AddItem(b, 10, 8m);

        order.ReplaceItems(new[] { (a, 20, 5m), (c, 5, 10m) });

        Assert.Equal(2, order.Items.Count);
        Assert.DoesNotContain(order.Items, i => i.ProductId == b);
        Assert.Equal(20, order.Items.Single(i => i.ProductId == a).Quantity);
        Assert.Equal(150m, order.TotalAmount);
    }

    [Fact]
    public void ReplaceItems_AfterBakingStarted_Throws()
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), 10, 5m);
        order.Status = OrderStatus.Baking;

        Assert.Throws<DomainException>(() => order.ReplaceItems(new[] { (Guid.NewGuid(), 1, 5m) }));
    }

    [Fact]
    public void ReplaceItems_WithInvalidQuantity_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.ReplaceItems(new[] { (Guid.NewGuid(), 0, 5m) }));
    }

    [Fact]
    public void ReplaceItems_WithNoLines_Throws()
    {
        var order = CreateOrder();

        Assert.Throws<DomainException>(() => order.ReplaceItems(Array.Empty<(Guid, int, decimal)>()));
    }
}