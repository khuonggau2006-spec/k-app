using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Locations.Commands.CreateLocation;
using TaskMgmt.Application.Features.Locations.Commands.DeleteLocation;
using TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;
using TaskMgmt.Application.Features.Locations.Common;
using TaskMgmt.Application.Features.Locations.Queries.GetLocationById;
using TaskMgmt.Application.Features.Locations.Queries.GetLocations;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/locations")]
public class LocationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<LocationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLocationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LocationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetLocationByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<LocationDto>> Create(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<LocationDto>> Update(Guid id, UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Id trên route và trong body không khớp.");
        }

        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteLocationCommand(id), cancellationToken);
        return NoContent();
    }
}
