namespace Non.Application.Common;

public static class CacheKeys
{
    public const string ActiveProducts = "products:active";

    public static string Product(Guid id) => $"products:{id}";

    public static string OtpCode(string phone) => $"auth:otp:{phone}";

    public static string OtpCooldown(string phone) => $"auth:otp-cooldown:{phone}";
}