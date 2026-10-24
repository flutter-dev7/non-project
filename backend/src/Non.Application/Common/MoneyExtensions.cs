using System.Globalization;

namespace Non.Application.Common;

public static class MoneyExtensions
{
    public static string ToMoney(this decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}