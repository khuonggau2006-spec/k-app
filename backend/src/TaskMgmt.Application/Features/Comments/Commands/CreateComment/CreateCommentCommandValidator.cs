using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Comments.Commands.CreateComment;

public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);

        RuleFor(x => x.WorkTaskId)
            .MustAsync(async (id, cancellationToken) =>
                await context.WorkTasks.AnyAsync(t => t.Id == id && t.IsActive, cancellationToken))
            .WithMessage("Công việc không tồn tại.");

        RuleForEach(x => x.MentionedUserIds)
            .MustAsync(async (userId, cancellationToken) =>
                await context.Users.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken))
            .WithMessage("Người dùng được nhắc đến không tồn tại.");
    }
}
