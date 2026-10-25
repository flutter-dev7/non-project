using System.Collections.Concurrent;
using Non.Application.Common;
using Non.Application.Interfaces.Services;

namespace Non.IntegrationTests.Infrastructure;

public class CapturingPushSender : IPushSender
{
    private readonly ConcurrentQueue<PushMessage> _sent = new();

    public IReadOnlyCollection<PushMessage> Sent => _sent.ToArray();

    public Task<PushResult> SendAsync(PushMessage message, CancellationToken ct = default)
    {
        _sent.Enqueue(message);

        var result = message.Token.StartsWith("invalid-", StringComparison.Ordinal)
            ? PushResult.InvalidToken
            : PushResult.Sent;

        return Task.FromResult(result);
    }

    public async Task<PushMessage> WaitForAsync(Func<PushMessage, bool> predicate, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);

        while (DateTime.UtcNow < deadline)
        {
            var found = _sent.ToArray().FirstOrDefault(predicate);

            if (found is not null)
                return found;

            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException("Push-уведомление не пришло за отведённое время.");
    }
}