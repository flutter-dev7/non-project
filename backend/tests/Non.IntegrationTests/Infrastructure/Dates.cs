namespace Non.IntegrationTests.Infrastructure;

public static class Dates
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(5));

    public static DateOnly Ahead(int days) => Today.AddDays(days);
}