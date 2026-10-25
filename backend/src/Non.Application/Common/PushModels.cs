namespace Non.Application.Common;

public enum PushResult
{
    Sent,
    InvalidToken,
    Failed
}

public record PushMessage(string Token, string Title, string Body, IReadOnlyDictionary<string, string> Data);

public record FcmTarget(Guid UserId, string Token);