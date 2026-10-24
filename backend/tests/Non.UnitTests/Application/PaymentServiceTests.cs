using Moq;
using Non.Application.Common;
using Non.Application.DTOs.Payments.Request;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Application.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Xunit;

namespace Non.UnitTests.Application;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IDebtService> _debts = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();

    private readonly Shop _shop = new() { Name = "Саодат", IsActive = true };
    private Payment? _saved;

    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);

        _shops
            .Setup(s => s.GetDetailsByIdAsync(_shop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_shop);

        _payments
            .Setup(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => _saved = p)
            .Returns(Task.CompletedTask);

        _payments
            .Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _saved);

        _service = new PaymentService(
            _payments.Object, _shops.Object, _orders.Object, _debts.Object, _unitOfWork.Object);
    }

    private void SetDebt(decimal delivered, decimal paid) =>
        _debts
            .Setup(d => d.GetBalanceAsync(_shop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShopBalance(delivered, paid, 0m));

    private CreatePaymentRequest Request(decimal amount) => new()
    {
        ShopId = _shop.Id,
        Amount = amount,
        Method = PaymentMethod.Cash
    };

    private static CurrentUser Caller(UserRole role) => new(Guid.NewGuid(), role);

    [Theory]
    [InlineData(UserRole.Shop)]
    [InlineData(UserRole.Agent)]
    public async Task Create_ByForbiddenRole_ReturnsForbidden(UserRole role)
    {
        var result = await _service.CreateAsync(Request(100m), Caller(role));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ShopNotFound_ReturnsNotFound()
    {
        var request = Request(100m);
        request.ShopId = Guid.NewGuid();

        var result = await _service.CreateAsync(request, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Create_CourierNotServingShop_ReturnsForbidden()
    {
        var courier = Caller(UserRole.Courier);
        _orders
            .Setup(o => o.CourierServesShopAsync(courier.Id, _shop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.CreateAsync(Request(100m), courier);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_AmountGreaterThanDebt_FailsWithoutSaving()
    {
        SetDebt(delivered: 500m, paid: 0m);

        var result = await _service.CreateAsync(Request(600m), Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_ByAdminWithinDebt_SavesAndCommits()
    {
        SetDebt(delivered: 900m, paid: 100m);
        var admin = Caller(UserRole.Admin);

        var result = await _service.CreateAsync(Request(300m), admin);

        Assert.True(result.IsSuccess);
        Assert.NotNull(_saved);
        Assert.Equal(300m, _saved!.Amount);
        Assert.Equal(admin.Id, _saved.ReceivedByUserId);
        _shops.Verify(s => s.LockForUpdateAsync(_shop.Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ByServingCourier_Succeeds()
    {
        SetDebt(delivered: 500m, paid: 0m);
        var courier = Caller(UserRole.Courier);
        _orders
            .Setup(o => o.CourierServesShopAsync(courier.Id, _shop.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(Request(500m), courier);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Reverse_ByNonAdmin_ReturnsForbidden()
    {
        var result = await _service.ReverseAsync(
            Guid.NewGuid(), new ReversePaymentRequest { Reason = "Ошибка" }, Caller(UserRole.Courier));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Reverse_PaymentNotFound_ReturnsNotFound()
    {
        _payments
            .Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var result = await _service.ReverseAsync(
            Guid.NewGuid(), new ReversePaymentRequest { Reason = "Ошибка" }, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Reverse_AlreadyReversed_ReturnsConflict()
    {
        var original = Payment.Create(_shop.Id, null, 300m, PaymentMethod.Cash, Guid.NewGuid());
        _payments
            .Setup(p => p.GetByIdAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);
        _payments
            .Setup(p => p.IsReversedAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.ReverseAsync(
            original.Id, new ReversePaymentRequest { Reason = "Ошибка" }, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        _payments.Verify(p => p.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reverse_OfReversal_Fails()
    {
        var reversal = Payment.Create(_shop.Id, null, 300m, PaymentMethod.Cash, Guid.NewGuid())
            .CreateReversal(Guid.NewGuid(), "Ошибка");
        _payments
            .Setup(p => p.GetByIdAsync(reversal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reversal);

        var result = await _service.ReverseAsync(
            reversal.Id, new ReversePaymentRequest { Reason = "Ещё раз" }, Caller(UserRole.Admin));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Reverse_ValidPayment_SavesNegativePayment()
    {
        var original = Payment.Create(_shop.Id, null, 300m, PaymentMethod.Cash, Guid.NewGuid());
        _payments
            .Setup(p => p.GetByIdAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);
        _payments
            .Setup(p => p.IsReversedAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.ReverseAsync(
            original.Id, new ReversePaymentRequest { Reason = "Ошибка ввода" }, Caller(UserRole.Admin));

        Assert.True(result.IsSuccess);
        Assert.NotNull(_saved);
        Assert.Equal(-300m, _saved!.Amount);
        Assert.Equal(original.Id, _saved.ReversalOfPaymentId);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}