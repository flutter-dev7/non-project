namespace Non.Application.Interfaces.Repositories;

public interface ISettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
}