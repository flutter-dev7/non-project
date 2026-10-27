namespace Non.Application.Common;

public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return $"Пароль должен содержать не менее {MinLength} символов.";

        if (password.Length > MaxLength)
            return $"Пароль слишком длинный (не более {MaxLength} символов).";

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "Пароль должен содержать и буквы, и цифры.";

        return null;
    }
}