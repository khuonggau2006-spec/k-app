using FluentValidation;

namespace TaskMgmt.Application.Features.Auth.Commands.RefreshAccessToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
