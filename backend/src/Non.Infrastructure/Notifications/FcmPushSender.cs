using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Non.Application.Common;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Notifications;

public sealed class FcmPushSender : IPushSender
{
    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FcmPushSender> _logger;

    public FcmPushSender(IConfiguration configuration, ILogger<FcmPushSender> logger)
    {
        _logger = logger;

        var json = configuration["Firebase:CredentialsJson"];
        var path = configuration["Firebase:CredentialsPath"];

        var credential = !string.IsNullOrWhiteSpace(json)
            ? GoogleCredential.FromJson(json)
            : GoogleCredential.FromFile(path);

        var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions { Credential = credential });

        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public async Task<PushResult> SendAsync(PushMessage message, CancellationToken ct = default)
    {
        var firebaseMessage = new Message
        {
            Token = message.Token,
            Notification = new Notification { Title = message.Title, Body = message.Body },
            Data = message.Data,
            Android = new AndroidConfig { Priority = Priority.High }
        };

        try
        {
            await _messaging.SendAsync(firebaseMessage, ct);

            return PushResult.Sent;
        }
        catch (FirebaseMessagingException ex) when (
            ex.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
        {
            _logger.LogWarning("FCM: токен недействителен ({Code}).", ex.MessagingErrorCode);

            return PushResult.InvalidToken;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FCM: не удалось отправить уведомление.");

            return PushResult.Failed;
        }
    }
}