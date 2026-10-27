using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Non.Application.Common;
using Non.Application.Interfaces.Services;

namespace Non.IntegrationTests.Infrastructure;

public class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new();
    private readonly ConcurrentDictionary<string, int> _counts = new();

    public bool FailNext { get; set; }

    public Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (FailNext)
        {
            FailNext = false;

            throw new EmailDeliveryException("Тестовый сбой отправки.");
        }

        var key = toEmail.ToLowerInvariant();
        var match = Regex.Match(htmlBody, @"(?<!\d)\d{6}(?!\d)");

        if (match.Success)
            _codes[key] = match.Value;

        _counts.AddOrUpdate(key, 1, (_, count) => count + 1);

        return Task.CompletedTask;
    }

    public string? GetCode(string email) =>
        _codes.TryGetValue(email.ToLowerInvariant(), out var code) ? code : null;

    public int SentCount(string email) =>
        _counts.TryGetValue(email.ToLowerInvariant(), out var count) ? count : 0;
}