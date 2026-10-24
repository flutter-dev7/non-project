using Non.Application.Common;
using Xunit;

namespace Non.UnitTests.Application;

public class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("+992900000001", "+992900000001")]
    [InlineData("+992 90 000 00 01", "+992900000001")]
    [InlineData("900000001", "+992900000001")]
    [InlineData("0900000001", "+992900000001")]
    [InlineData("992900000001", "+992900000001")]
    [InlineData("(900) 000-001", "+992900000001")]
    public void Normalize_ValidNumbers_ReturnsInternationalFormat(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("+79991234567")]
    [InlineData("+9929000000011")]
    public void Normalize_InvalidNumbers_ReturnsNull(string? input)
    {
        Assert.Null(PhoneNumberNormalizer.Normalize(input));
    }
}