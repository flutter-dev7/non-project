using Non.Domain.Common;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Xunit;

namespace Non.UnitTests.Domain;

public class PaymentTests
{
    private static Payment CreateValid(decimal amount = 100m) =>
        Payment.Create(Guid.NewGuid(), null, amount, PaymentMethod.Cash, Guid.NewGuid());

    [Fact]
    public void Create_WithValidData_SetsFields()
    {
        var shopId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var payment = Payment.Create(shopId, null, 150.50m, PaymentMethod.Transfer, userId);

        Assert.Equal(shopId, payment.ShopId);
        Assert.Equal(150.50m, payment.Amount);
        Assert.Equal(PaymentMethod.Transfer, payment.Method);
        Assert.Equal(userId, payment.ReceivedByUserId);
        Assert.False(payment.IsReversal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.5)]
    public void Create_WithNonPositiveAmount_Throws(double amount)
    {
        Assert.Throws<DomainException>(() => CreateValid((decimal)amount));
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimals_Throws()
    {
        Assert.Throws<DomainException>(() => CreateValid(10.123m));
    }

    [Fact]
    public void CreateReversal_NegatesAmountAndLinksToOriginal()
    {
        var payment = CreateValid(300m);
        var adminId = Guid.NewGuid();

        var reversal = payment.CreateReversal(adminId, "  Ошибка ввода  ");

        Assert.Equal(-300m, reversal.Amount);
        Assert.Equal(payment.ShopId, reversal.ShopId);
        Assert.Equal(payment.Id, reversal.ReversalOfPaymentId);
        Assert.Equal("Ошибка ввода", reversal.ReversalReason);
        Assert.Equal(adminId, reversal.ReceivedByUserId);
        Assert.True(reversal.IsReversal);
    }

    [Fact]
    public void CreateReversal_OfReversal_Throws()
    {
        var reversal = CreateValid().CreateReversal(Guid.NewGuid(), "Ошибка");

        Assert.Throws<DomainException>(() => reversal.CreateReversal(Guid.NewGuid(), "Ещё раз"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateReversal_WithoutReason_Throws(string reason)
    {
        Assert.Throws<DomainException>(() => CreateValid().CreateReversal(Guid.NewGuid(), reason));
    }
}