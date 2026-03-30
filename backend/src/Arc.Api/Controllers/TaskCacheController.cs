using Microsoft.AspNetCore.Mvc;
using Arc.Application.Execution;
namespace Arc.Api.Controllers;


[ApiController]
[Route("api/cache")]
public sealed class TaskCacheController : ControllerBase
{
    private readonly ITaskExecutionCache _cache;

    public TaskCacheController(ITaskExecutionCache cache)
    {
        _cache = cache;
    }

    [HttpDelete]
    public async Task<IActionResult> Invalidate([FromQuery] string? taskHash = null)
    {
        await _cache.InvalidateAsync(taskHash);
        return NoContent();
    }
}