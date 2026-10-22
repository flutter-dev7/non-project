namespace Non.Application.Common;

public record JwtToken(string Value, DateTime ExpiresAt);