using Microsoft.Extensions.Logging;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Services;

public class LogSmsSender : ISmsSender
{
    private readonly ILogger<LogSmsSender> _logger;

    public LogSmsSender(ILogger<LogSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phone, string message, CancellationToken ct = default)
    {
        _logger.LogWarning("SMS (заглушка) на {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}