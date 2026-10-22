using Microsoft.EntityFrameworkCore;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default) =>
        _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Phone == phone, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }
}