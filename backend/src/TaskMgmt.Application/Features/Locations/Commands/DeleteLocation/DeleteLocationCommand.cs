using MediatR;

namespace TaskMgmt.Application.Features.Locations.Commands.DeleteLocation;

public record DeleteLocationCommand(Guid Id) : IRequest;
