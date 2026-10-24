using Moq;
using Non.Application.Common;
using Non.Application.DTOs.Orders.Request;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Application.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Non.UnitTests.Helpers;
using Xunit;

namespace Non.UnitTests.Application;

public class OrderServiceTests
{
    private static readonly DateOnly DeliveryDate = new(2026, 10, 30);

    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISettingRepository> _settings = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IDebtService> _debts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<ICacheService> _cache = new();

    private readonly Shop _shop = new() { Name = "Саодат", IsActive = true };
    private readonly Product _product = new() { Name = "Нони тандури", Price = 5m, IsActive = true };

    private Order? _savedOrder;
    private Payment? _savedPayment;

    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);

        _shops
            .Setup(s => s.GetDetailsByIdAsync(_shop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_shop);

        _products
            .Setup(p => p.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { _product });

        _orders
            .Setup(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => _savedOrder = o)
            .Returns(Task.CompletedTask);

        _orders
            .Setup(o => o.GetDetailsByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _savedOrder);

        _payments
            .Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => _savedPayment = p)
            .Returns(Task.CompletedTask);

        var now = new DateTimeOffset(2026, 10, 24, 5, 0, 0, TimeSpan.Zero);

        _service = new OrderService(
            _orders.Object, _shops.Object, _products.Object, _users.Object, _settings.Object,
            _payments.Object, _debts.Object, _unitOfWork.Object, _cache.Object, new FixedTimeProvider(now));
    }

    private void SetBalance(decimal delivered, decimal paid, decimal pending = 0m) =>
        _debts
            .Setup(d => d.GetBalanceAsync(_shop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopBalance(delivered, paid, pending));

    private CreateOrderRequest CreateRequest(PaymentType paymentType, int quantity) => new()
    {
        ShopId = _shop.Id,
        DeliveryDate = DeliveryDate,
        PaymentType = paymentType,
        Items = new List<CreateOrderItemRequest>
        {
            new() { ProductId = _product.Id, Quantity = quantity }
        }
    };

    private static CurrentUser Caller(UserRole role) => new(Guid.NewGuid(), role);

    [Fact]
    public async Task Create_DebtOrderOverLimit_IsRejected()
    {
        _shop.DebtLimit = 1000m;
        SetBalance(delivered: 600m, paid: 0m);

        var result = await _service.CreateAsync(CreateRequest(PaymentType.Debt, 100), Caller(UserRole.Agent));
        Assert.True(result.IsFailure);
        _orders.Verify(o => o.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_DebtOrderCountsPendingDebtOrders()
    {
        _shop.DebtLimit = 1000m;
        SetBalance(delivered: 300m, paid: 0m, pending: 400m);

        var result = await _service.CreateAsync(CreateRequest(PaymentType.Debt, 70), Caller(UserRole.Agent));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Create_DebtOrderWithinLimit_Succeeds()
    {
        _shop.DebtLimit = 1000m;
        SetBalance(delivered: 600m, paid: 0m);

        var result = await _service.CreateAsync(CreateRequest(PaymentType.Debt, 50), Caller(UserRole.Agent));

        Assert.True(result.IsSuccess);
        Assert.NotNull(_savedOrder);
        Assert.Equal(250m, _savedOrder!.TotalAmount);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_AdminBypassesDebtLimit()
    {
        _shop.DebtLimit = 100m;
        SetBalance(delivered: 600m, paid: 0m);

        var result = await _service.CreateAsync(CreateRequest(PaymentType.Debt, 100), Caller(UserRole.Admin));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_NowOrderIgnoresDebtLimit()
    {
        _shop.DebtLimit = 100m;
        SetBalance(delivered: 600m, paid: 0m);

        var result = await _service.CreateAsync(CreateRequest(PaymentType.Now, 100), Caller(UserRole.Agent));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_DeliveryDateTomorrow_AfterCutoff_IsRejectedForAgent()
    {
        var request = CreateRequest(PaymentType.Now, 10);
        request.DeliveryDate = new DateOnly(2026, 10, 25);

        var result = await _service.CreateAsync(request, Caller(UserRole.Agent));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Create_DeliveryDateInPast_IsRejected()
    {
        var request = CreateRequest(PaymentType.Now, 10);
        request.DeliveryDate = new DateOnly(2026, 10, 24);

        var result = await _service.CreateAsync(request, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Create_DeliveryDateMoreThan30DaysAhead_IsRejected()
    {
        var request = CreateRequest(PaymentType.Now, 10);
        request.DeliveryDate = new DateOnly(2026, 12, 31);

        var result = await _service.CreateAsync(request, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Create_InactiveProduct_IsRejected()
    {
        _product.IsActive = false;

        var result = await _service.CreateAsync(CreateRequest(PaymentType.Now, 10), Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
    }

    private Order OnTheWayOrder(Guid courierId, PaymentType paymentType, int quantity = 20)
    {
        var order = new Order
        {
            ShopId = _shop.Id,
            CourierId = courierId,
            DeliveryDate = DeliveryDate,
            PaymentType = paymentType,
            Status = OrderStatus.OnTheWay
        };
        order.AddItem(_product.Id, quantity, _product.Price);

        _orders
            .Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _orders
            .Setup(o => o.GetDetailsByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        return order;
    }

    [Fact]
    public async Task Deliver_NowOrderWithoutAmount_Fails()
    {
        var courier = Caller(UserRole.Courier);
        var order = OnTheWayOrder(courier.Id, PaymentType.Now);

        var result = await _service.ChangeStatusAsync(
            order.Id, new ChangeOrderStatusRequest { Status = OrderStatus.Delivered }, courier);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.OnTheWay, order.Status);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deliver_NowOrderWithAmount_CreatesPaymentAndCommits()
    {
        var courier = Caller(UserRole.Courier);
        var order = OnTheWayOrder(courier.Id, PaymentType.Now);
        SetBalance(delivered: 0m, paid: 0m);

        var result = await _service.ChangeStatusAsync(
            order.Id,
            new ChangeOrderStatusRequest
            {
                Status = OrderStatus.Delivered,
                PaymentAmount = 100m,
                PaymentMethod = PaymentMethod.Cash
            },
            courier);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(_savedPayment);
        Assert.Equal(100m, _savedPayment!.Amount);
        Assert.Equal(order.Id, _savedPayment.OrderId);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deliver_AmountGreaterThanDebt_FailsWithoutPayment()
    {
        var courier = Caller(UserRole.Courier);
        var order = OnTheWayOrder(courier.Id, PaymentType.Now);
        SetBalance(delivered: 0m, paid: 0m);

        var result = await _service.ChangeStatusAsync(
            order.Id,
            new ChangeOrderStatusRequest { Status = OrderStatus.Delivered, PaymentAmount = 500m },
            courier);

        Assert.True(result.IsFailure);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deliver_DebtOrderWithoutAmount_Succeeds()
    {
        var courier = Caller(UserRole.Courier);
        var order = OnTheWayOrder(courier.Id, PaymentType.Debt);

        var result = await _service.ChangeStatusAsync(
            order.Id, new ChangeOrderStatusRequest { Status = OrderStatus.Delivered }, courier);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatus_CourierOnForeignOrder_ReturnsForbidden()
    {
        var order = OnTheWayOrder(Guid.NewGuid(), PaymentType.Debt);

        var result = await _service.ChangeStatusAsync(
            order.Id, new ChangeOrderStatusRequest { Status = OrderStatus.Delivered }, Caller(UserRole.Courier));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task ChangeStatus_PaymentAmountOutsideDelivery_Fails()
    {
        var order = OnTheWayOrder(Guid.NewGuid(), PaymentType.Debt);

        var result = await _service.ChangeStatusAsync(
            order.Id,
            new ChangeOrderStatusRequest { Status = OrderStatus.OnTheWay, PaymentAmount = 100m },
            Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ChangeStatus_ToCancelled_IsNotAllowedThroughStatusEndpoint()
    {
        var result = await _service.ChangeStatusAsync(
            Guid.NewGuid(), new ChangeOrderStatusRequest { Status = OrderStatus.Cancelled }, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
    }
}