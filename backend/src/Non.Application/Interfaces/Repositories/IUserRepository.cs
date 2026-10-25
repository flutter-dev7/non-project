using Non.Application.Common;
using Non.Domain.Entities;
using Non.Domain.Enums;

namespace Non.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);

    Task<(List<User> Items, int TotalCount)> GetPagedAsync(
        string? search, UserRole? role, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    
    Task<List<User>> GetTrackedByFcmTokenAsync(string token, CancellationToken ct = default);

    Task<List<FcmTarget>> GetFcmTargetsByRoleAsync(UserRole role, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}