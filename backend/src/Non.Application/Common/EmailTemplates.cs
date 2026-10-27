using System.Net;

namespace Non.Application.Common;

public static class EmailTemplates
{
    public static (string Subject, string Html) Verification(string name, string code, int lifetimeMinutes) =>
        ("Нон: код подтверждения email",
            Build(name, "Ваш код подтверждения email:", code, lifetimeMinutes,
                "Если вы не регистрировались в приложении «Нон», просто проигнорируйте это письмо."));

    public static (string Subject, string Html) PasswordReset(string name, string code, int lifetimeMinutes) =>
        ("Нон: код для смены пароля",
            Build(name, "Ваш код для смены пароля:", code, lifetimeMinutes,
                "Если вы не запрашивали смену пароля, просто проигнорируйте это письмо: пароль останется прежним."));

    private static string Build(string name, string lead, string code, int lifetimeMinutes, string footer) =>
        $"<p>Здравствуйте, {WebUtility.HtmlEncode(name)}!</p>" +
        $"<p>{lead}</p>" +
        $"<p style=\"font-size:24px;letter-spacing:4px\"><b>{code}</b></p>" +
        $"<p>Код действует {lifetimeMinutes} минут. Никому не сообщайте его.</p>" +
        $"<p>{footer}</p>";
}