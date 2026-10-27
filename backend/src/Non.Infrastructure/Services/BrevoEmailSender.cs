using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Non.Application.Common;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Services;

public sealed class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly ILogger<BrevoEmailSender> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public BrevoEmailSender(HttpClient http, IConfiguration configuration, ILogger<BrevoEmailSender> logger)
    {
        _http = http;
        _logger = logger;

        _fromEmail = configuration["Email:FromEmail"]
            ?? throw new InvalidOperationException("Не задан Email:FromEmail.");
        _fromName = configuration["Email:FromName"] ?? "Нон";

        _http.BaseAddress = new Uri("https://api.brevo.com/v3/");
        _http.Timeout = TimeSpan.FromSeconds(15);
        _http.DefaultRequestHeaders.Add("api-key", configuration["Email:BrevoApiKey"]);
    }

    public async Task SendAsync(
        string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        var payload = new
        {
            sender = new { name = _fromName, email = _fromEmail },
            to = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = htmlBody
        };

        try
        {
            using var response = await _http.PostAsJsonAsync("smtp/email", payload, ct);

            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Brevo отклонил письмо: {Status} {Body}", (int)response.StatusCode, body);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Не удалось связаться с Brevo.");
        }

        throw new EmailDeliveryException("Не удалось отправить письмо.");
    }
}