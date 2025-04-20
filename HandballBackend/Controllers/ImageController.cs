using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/")]
public class ImageController : ControllerBase {
    // GET api/values
    [HttpGet("image")]
    public IActionResult Get([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "");
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }

    [HttpGet("tournaments/image")]
    public IActionResult GetTournaments([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "") + "tournaments/";
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }

    [HttpGet("teams/image")]
    public IActionResult GetTeams([BindRequired, FromQuery] string name, [FromQuery] bool big) {
        var fileName = Uri.EscapeDataString(name);
        var path = "./resources/images/" + (big ? "big/" : "") + "teams/";
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }
}