namespace Non.Application.Interfaces.Services;

public interface IUserAccessService
{
    Task<bool> IsAllowedAsync(Guid userId, string role, CancellationToken ct = default);
}