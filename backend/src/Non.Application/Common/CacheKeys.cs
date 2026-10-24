namespace Non.Application.Common;

public static class CacheKeys
{
    public const string ActiveProducts = "products:active";
    public const string OrderCutoffTime = "settings:order-cutoff-time";

    public static string Product(Guid id) => $"products:{id}";

    public static string OtpCode(string phone) => $"auth:otp:{phone}";

    public static string OtpCooldown(string phone) => $"auth:otp-cooldown:{phone}";
    
    public static string UserAccess(Guid userId) => $"auth:user-access:{userId}";
}