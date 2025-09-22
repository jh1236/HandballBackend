using HandballBackend.Authentication;
using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Authorize(Policy = Policies.IsAdmin)]
[Route("/api/[controller]")]
public class TestController : ControllerBase {
    [HttpPost("backup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> CreateBackup() {
        await PostgresBackup.MakeTimestampedBackup("requested", force: true);
        return Ok();
    }

    [HttpPost("update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult UpdateFromGit() {
        _ = Task.Run(async () => {
            await Task.Delay(200);
            ServerManagmentHelper.UpdateServer();
        });
        return Ok();
    }

    [HttpPost("restart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult RestartServer() {
        _ = Task.Run(async () => {
            await Task.Delay(200);
            ServerManagmentHelper.RestartServer();
        });
        return Ok();
    }

    [HttpPost("rebuild")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult RebuildServer() {
        _ = Task.Run(async () => {
            await Task.Delay(200);
            ServerManagmentHelper.RebuildServer();
        });
        return Ok();
    }

    [HttpGet("log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> GetLog() {
        return await ExceptionLoggingHelper.Read();
    }

    [HttpPost("log/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ClearLog() {
        await ExceptionLoggingHelper.Clear();
        return Ok();
    }
}