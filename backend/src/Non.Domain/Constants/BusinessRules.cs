namespace Non.Domain.Constants;

public static class BusinessRules
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(5);

    public const int MaxDeliveryDaysAhead = 30;
}