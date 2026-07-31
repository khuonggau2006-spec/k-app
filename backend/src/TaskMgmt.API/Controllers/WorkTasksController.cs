using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTaskById;
using TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTasks;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/worktasks")]
public class WorkTasksController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkTaskDto>>> GetAll(
        [FromQuery] GetWorkTasksQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkTaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkTaskByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<WorkTaskDto>> Create(CreateWorkTaskCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkTaskDto>> Update(Guid id, UpdateWorkTaskCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Id trên route và trong body không khớp.");
        }

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWorkTaskCommand(id), cancellationToken);
        return NoContent();
    }
}
