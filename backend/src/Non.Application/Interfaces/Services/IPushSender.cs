using Non.Application.Common;

namespace Non.Application.Interfaces.Services;

public interface IPushSender
{
    Task<PushResult> SendAsync(PushMessage message, CancellationToken ct = default);
}