using TaskMgmt.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.DeviceTokens;

public class RegisterDeviceTokenCommandValidatorTests
{
    private readonly RegisterDeviceTokenCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ValidToken_IsValid()
    {
        var command = new RegisterDeviceTokenCommand("some-fcm-token-value", DevicePlatform.Android);

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyToken_IsInvalid()
    {
        var command = new RegisterDeviceTokenCommand("", DevicePlatform.Ios);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterDeviceTokenCommand.Token));
    }

    [Fact]
    public async Task Validate_TokenTooLong_IsInvalid()
    {
        var command = new RegisterDeviceTokenCommand(new string('a', 4097), DevicePlatform.Web);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterDeviceTokenCommand.Token));
    }
}
