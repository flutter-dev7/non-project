namespace Non.Application.Common;

public static class AuthCacheKeys
{
    public static string Code(EmailCodePurpose purpose, string email) =>
        $"auth:code:{(int)purpose}:{email}";

    public static string Cooldown(EmailCodePurpose purpose, string email) =>
        $"auth:code-cooldown:{(int)purpose}:{email}";

    public static string LoginFailures(string email) => $"auth:login-failures:{email}";

    public static string PasswordChangeFailures(Guid userId) => $"auth:change-failures:{userId}";

    public static string ResetToken(string tokenHash) => $"auth:reset-token:{tokenHash}";
}