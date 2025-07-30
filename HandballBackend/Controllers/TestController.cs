using HandballBackend.Authentication;
using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TestController : ControllerBase {
    [HttpGet("Mirror")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, string>> Mirror([FromQuery] Dictionary<string, string> input) {
        return input;
    }

    [Authorize(Policy = Policies.IsAdmin)]
    [HttpPost("Backup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> CreateBackup() {
        await PostgresBackup.MakeTimestampedBackup("requested", force: true);
        return Ok();
    }
}