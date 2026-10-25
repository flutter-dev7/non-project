using Non.Application.Common;
using Non.Domain.Entities;
using Xunit;

namespace Non.UnitTests.Application;

public class RefreshTokenTests
{
    [Fact]
    public void Generate_ReturnsUniqueUrlSafeTokens()
    {
        var first = RefreshTokenHasher.Generate();
        var second = RefreshTokenHasher.Generate();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain('+', first);
        Assert.DoesNotContain('/', first);
        Assert.DoesNotContain('=', first);
        Assert.True(first.Length >= 80);
    }

    [Fact]
    public void Hash_IsDeterministic_AndDiffersFromToken()
    {
        var token = RefreshTokenHasher.Generate();

        Assert.Equal(RefreshTokenHasher.Hash(token), RefreshTokenHasher.Hash(token));
        Assert.NotEqual(token, RefreshTokenHasher.Hash(token));
        Assert.Equal(64, RefreshTokenHasher.Hash(token).Length);
    }

    [Fact]
    public void IsActive_DependsOnExpiryAndRevocation()
    {
        var now = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        var active = new RefreshToken { ExpiresAt = now.AddDays(1) };
        var expired = new RefreshToken { ExpiresAt = now.AddSeconds(-1) };
        var revoked = new RefreshToken { ExpiresAt = now.AddDays(1), RevokedAt = now };

        Assert.True(active.IsActive(now));
        Assert.False(expired.IsActive(now));
        Assert.False(revoked.IsActive(now));
    }
}