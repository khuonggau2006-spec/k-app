using FluentValidation;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;

public class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.Type)
            .Must(type => NotificationTypes.All.Contains(type))
            .WithMessage("Loại thông báo không hợp lệ.");
    }
}
