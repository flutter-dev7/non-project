using Non.Application.Common;

namespace Non.Application.Interfaces.Services;

public interface IPushQueue
{
    void Enqueue(PushMessage message);
}