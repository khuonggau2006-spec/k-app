using Microsoft.Extensions.Logging.Abstractions;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Infrastructure.BackgroundJobs;

namespace TaskMgmt.Application.UnitTests.BackgroundJobs;

public class SendPushNotificationJobTests
{
    [Fact]
    public async Task Execute_CallsPushNotificationServiceWithGivenArguments()
    {
        var push = new FakePushNotificationService();
        var job = new SendPushNotificationJob(push, NullLogger<SendPushNotificationJob>.Instance);
        var userId = Guid.NewGuid();

        await job.ExecuteAsync(userId, "Tiêu đề", "Nội dung", new Dictionary<string, string> { ["workTaskId"] = "abc" });

        var sent = Assert.Single(push.Sent);
        Assert.Equal(userId, sent.UserId);
        Assert.Equal("Tiêu đề", sent.Title);
        Assert.Equal("Nội dung", sent.Body);
    }

    [Fact]
    public async Task Execute_NullData_StillSends()
    {
        var push = new FakePushNotificationService();
        var job = new SendPushNotificationJob(push, NullLogger<SendPushNotificationJob>.Instance);

        await job.ExecuteAsync(Guid.NewGuid(), "Tiêu đề", "Nội dung", null);

        Assert.Single(push.Sent);
    }
}
