using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;
using TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;
using TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Application.Features.TaskAssignees.Queries.GetTaskAssignees;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/worktasks/{taskId:guid}/assignees")]
public class TaskAssigneesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TaskAssigneeDto>>> GetAll(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTaskAssigneesQuery(taskId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TaskAssigneeDto>> Add(
        Guid taskId, AddTaskAssigneeRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddTaskAssigneeCommand(taskId, request.UserId, request.Role), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { taskId }, result);
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<TaskAssigneeDto>> ChangeRole(
        Guid taskId, Guid userId, ChangeTaskAssigneeRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChangeTaskAssigneeRoleCommand(taskId, userId, request.Role), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid taskId, Guid userId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveTaskAssigneeCommand(taskId, userId), cancellationToken);
        return NoContent();
    }
}

public record AddTaskAssigneeRequest(Guid UserId, TaskAssigneeRole Role);

public record ChangeTaskAssigneeRoleRequest(TaskAssigneeRole Role);
