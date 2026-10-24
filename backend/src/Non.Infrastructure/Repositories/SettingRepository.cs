using Microsoft.EntityFrameworkCore;
using Non.Application.Interfaces.Repositories;
using Non.Domain.Entities;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Repositories;

public class SettingRepository : ISettingRepository
{
    private readonly AppDbContext _context;

    public SettingRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<string?> GetValueAsync(string key, CancellationToken ct = default) =>
        _context.Settings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => (string?)s.Value)
            .FirstOrDefaultAsync(ct);

    public async Task SetValueAsync(string key, string value, CancellationToken ct = default)
    {
        var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);

        if (setting is null)
            await _context.Settings.AddAsync(new Setting { Key = key, Value = value }, ct);
        else
            setting.Value = value;
    }
}