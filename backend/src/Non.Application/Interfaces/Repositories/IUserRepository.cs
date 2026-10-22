using Non.Domain.Entities;

namespace Non.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);
}