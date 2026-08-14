using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.Notifications;

public class FirebaseFcmPushNotificationService(
    IApplicationDbContext context,
    FirebaseAppAccessor firebaseAppAccessor,
    ILogger<FirebaseFcmPushNotificationService> logger) : IPushNotificationService
{
    private readonly FirebaseMessaging? _messaging =
        firebaseAppAccessor.App is null ? null : FirebaseMessaging.GetMessaging(firebaseAppAccessor.App);

    public async Task<PushSendResult> SendToUserAsync(
        Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data, CancellationToken cancellationToken)
    {
        var tokens = await context.DeviceTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        return await SendToTokensAsync(tokens, title, body, data, cancellationToken);
    }

    private async Task<PushSendResult> SendToTokensAsync(
        List<string> tokens, string title, string body, IReadOnlyDictionary<string, string>? data, CancellationToken cancellationToken)
    {
        if (_messaging is null)
        {
            logger.LogWarning("Firebase chưa được cấu hình (thiếu Firebase:CredentialsPath) - bỏ qua gửi push.");
            return new PushSendResult(0, 0);
        }

        if (tokens.Count == 0)
        {
            return new PushSendResult(0, 0);
        }

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification { Title = title, Body = body },
            Data = data,
        };

        var response = await _messaging.SendEachForMulticastAsync(message, cancellationToken);

        await RemoveInvalidTokensAsync(tokens, response, cancellationToken);

        return new PushSendResult(response.SuccessCount, response.FailureCount);
    }

    // Token bị thu hồi (gỡ app/đổi thiết bị) hoặc sai định dạng thì không thể phục hồi -
    // dọn khỏi DB để lần gửi sau không lãng phí request tới FCM cho token đã chết.
    private async Task RemoveInvalidTokensAsync(List<string> tokens, BatchResponse response, CancellationToken cancellationToken)
    {
        var invalidTokens = new List<string>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            if (!result.IsSuccess && result.Exception is FirebaseMessagingException
                {
                    MessagingErrorCode: MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument,
                })
            {
                invalidTokens.Add(tokens[i]);
            }
        }

        if (invalidTokens.Count == 0)
        {
            return;
        }

        var toRemove = await context.DeviceTokens.Where(t => invalidTokens.Contains(t.Token)).ToListAsync(cancellationToken);
        context.DeviceTokens.RemoveRange(toRemove);
        await context.SaveChangesAsync(cancellationToken);
    }
}
