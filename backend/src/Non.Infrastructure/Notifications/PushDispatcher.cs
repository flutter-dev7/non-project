using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Notifications;

public sealed class PushDispatcher : BackgroundService
{
    private readonly PushQueue _queue;
    private readonly IPushSender _sender;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PushDispatcher> _logger;

    public PushDispatcher(
        PushQueue queue,
        IPushSender sender,
        IServiceScopeFactory scopeFactory,
        ILogger<PushDispatcher> logger)
    {
        _queue = queue;
        _sender = sender;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var result = await _sender.SendAsync(message, stoppingToken);

                    if (result == PushResult.InvalidToken)
                        await ClearInvalidTokenAsync(message.Token, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Не удалось отправить push-уведомление.");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ClearInvalidTokenAsync(string token, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var owners = await users.GetTrackedByFcmTokenAsync(token, ct);

        foreach (var user in owners)
            user.FcmToken = null;

        await unitOfWork.SaveChangesAsync(ct);
    }
}