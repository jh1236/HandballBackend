using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TestController : ControllerBase {
    [HttpGet("Mirror")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, string>> Mirror(
        [FromQuery] Dictionary<string, string> input
    ) {
        return input;
    }
}