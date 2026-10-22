namespace Non.Application.Common;

public static class PhoneNumberNormalizer
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (digits.Length == 10 && digits.StartsWith('0'))
            digits = digits[1..];

        if (digits.Length == 9)
            digits = "992" + digits;

        if (digits.Length != 12 || !digits.StartsWith("992"))
            return null;

        return "+" + digits;
    }
}