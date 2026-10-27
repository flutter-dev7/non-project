using Non.Application.Common;
using Xunit;

namespace Non.UnitTests.Application;

public class PasswordRulesTests
{
    [Theory]
    [InlineData("Secret123")]
    [InlineData("пароль2026")]
    [InlineData("abcdefg1")]
    public void Validate_StrongEnough_ReturnsNull(string password)
    {
        Assert.Null(PasswordRules.Validate(password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc12")]
    [InlineData("onlyletters")]
    [InlineData("1234567890")]
    public void Validate_WeakPassword_ReturnsError(string? password)
    {
        Assert.NotNull(PasswordRules.Validate(password));
    }

    [Fact]
    public void Validate_TooLong_ReturnsError()
    {
        var password = new string('a', 100) + "1";

        Assert.NotNull(PasswordRules.Validate(password));
    }
}