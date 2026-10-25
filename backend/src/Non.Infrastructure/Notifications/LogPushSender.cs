using Microsoft.Extensions.Logging;
using Non.Application.Common;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Notifications;

public sealed class LogPushSender : IPushSender
{
    private readonly ILogger<LogPushSender> _logger;

    public LogPushSender(ILogger<LogPushSender> logger)
    {
        _logger = logger;
    }

    public Task<PushResult> SendAsync(PushMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation("Push (заглушка, Firebase не настроен): {Title} | {Body}", message.Title, message.Body);

        return Task.FromResult(PushResult.Sent);
    }
}