namespace Non.Application.Interfaces.Repositories;

public interface ISettingRepository
{
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);

    Task SetValueAsync(string key, string value, CancellationToken ct = default);
}