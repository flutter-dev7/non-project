using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;

namespace Non.Application.Services;

public record UserAccessState(bool IsActive, string Role);

public class UserAccessService : IUserAccessService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;

    public UserAccessService(IUserRepository userRepository, ICacheService cacheService)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
    }

    public async Task<bool> IsAllowedAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var state = await _cacheService.GetOrCreateAsync(
            CacheKeys.UserAccess(userId),
            async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId, ct);

                return user is null
                    ? new UserAccessState(false, string.Empty)
                    : new UserAccessState(user.IsActive, user.Role.ToString());
            },
            CacheDuration);

        return state.IsActive && state.Role == role;
    }
}