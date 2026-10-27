using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Services;

public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var text = Regex.Replace(htmlBody, "<.*?>", " ");

        _logger.LogWarning("Письмо (заглушка, Brevo не настроен) для {Email}: {Subject} | {Text}", toEmail, subject, text);

        return Task.CompletedTask;
    }
}