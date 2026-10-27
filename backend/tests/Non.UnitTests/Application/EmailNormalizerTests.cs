using Non.Application.Common;
using Xunit;

namespace Non.UnitTests.Application;

public class EmailNormalizerTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("  User@Example.COM  ", "user@example.com")]
    [InlineData("first.last+tag@mail.example.org", "first.last+tag@mail.example.org")]
    public void Normalize_ValidEmail_ReturnsLowerCaseTrimmed(string input, string expected)
    {
        Assert.Equal(expected, EmailNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plainaddress")]
    [InlineData("user@localhost")]
    [InlineData("user @example.com")]
    [InlineData("Имя <user@example.com>")]
    [InlineData("@example.com")]
    public void Normalize_InvalidEmail_ReturnsNull(string? input)
    {
        Assert.Null(EmailNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_TooLongEmail_ReturnsNull()
    {
        var input = new string('a', 250) + "@example.com";

        Assert.Null(EmailNormalizer.Normalize(input));
    }
}