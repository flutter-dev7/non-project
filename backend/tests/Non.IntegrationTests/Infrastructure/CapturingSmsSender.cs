using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Non.Application.Interfaces.Services;

namespace Non.IntegrationTests.Infrastructure;

public class CapturingSmsSender : ISmsSender
{
    private readonly ConcurrentDictionary<string, string> _codes = new();

    public Task SendAsync(string phone, string message, CancellationToken ct = default)
    {
        var match = Regex.Match(message, @"\d{6}");

        if (match.Success)
            _codes[phone] = match.Value;

        return Task.CompletedTask;
    }

    public string? GetCode(string phone) => _codes.TryGetValue(phone, out var code) ? code : null;
}