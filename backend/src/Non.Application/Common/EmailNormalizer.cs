using System.Net.Mail;

namespace Non.Application.Common;

public static class EmailNormalizer
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim().ToLowerInvariant();

        if (value.Length > 254 || value.Contains(' '))
            return null;

        if (!MailAddress.TryCreate(value, out var address) || address.Address != value)
            return null;

        if (!address.Host.Contains('.'))
            return null;

        return value;
    }
}