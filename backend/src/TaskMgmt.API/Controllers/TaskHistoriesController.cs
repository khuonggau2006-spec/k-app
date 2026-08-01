using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.TaskHistories.Common;
using TaskMgmt.Application.Features.TaskHistories.Queries.GetTaskHistory;

namespace TaskMgmt.API.Controllers;

// Chỉ có GET - lịch sử là dữ liệu chỉ-ghi, không có endpoint sửa/xoá kể cả cho Admin.
[ApiController]
[Route("api/v1/worktasks/{taskId:guid}/history")]
public class TaskHistoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskHistoryDto>>> GetAll(
        Guid taskId, [FromQuery] GetTaskHistoryQuery query, CancellationToken cancellationToken)
    {
        if (taskId != query.WorkTaskId)
        {
            query = query with { WorkTaskId = taskId };
        }

        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
