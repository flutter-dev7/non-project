using Moq;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Application.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Xunit;

namespace Non.UnitTests.Application;

public class UserAccessServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly UserAccessService _service;

    public UserAccessServiceTests()
    {
        _cache
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(), It.IsAny<Func<Task<UserAccessState>>>(), It.IsAny<TimeSpan?>()))
            .Returns((string _, Func<Task<UserAccessState>> factory, TimeSpan? _) => factory());

        _service = new UserAccessService(_users.Object, _cache.Object);
    }

    private User UserWith(UserRole role, bool isActive)
    {
        var user = new User { FirstName = "Тест", LastName = "Тестов", Phone = "+992900000001", Role = role, IsActive = isActive };

        _users
            .Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        return user;
    }

    [Fact]
    public async Task ActiveUserWithSameRole_IsAllowed()
    {
        var user = UserWith(UserRole.Courier, true);

        Assert.True(await _service.IsAllowedAsync(user.Id, "Courier"));
    }

    [Fact]
    public async Task InactiveUser_IsDenied()
    {
        var user = UserWith(UserRole.Courier, false);

        Assert.False(await _service.IsAllowedAsync(user.Id, "Courier"));
    }

    [Fact]
    public async Task ChangedRole_IsDenied()
    {
        var user = UserWith(UserRole.Agent, true);

        Assert.False(await _service.IsAllowedAsync(user.Id, "Admin"));
    }

    [Fact]
    public async Task UnknownUser_IsDenied()
    {
        _users
            .Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Assert.False(await _service.IsAllowedAsync(Guid.NewGuid(), "Admin"));
    }
}