using MediatR;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;

public record DeleteWorkTaskCommand(Guid Id) : IRequest;
