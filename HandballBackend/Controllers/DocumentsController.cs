using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase {
    // GET api/values
    [HttpGet("rules")]
    public IActionResult Rules() {
        return File(
            System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/rules.pdf"),
            "application/pdf"
        );
    }

    [HttpGet("simplified_rules")]
    public IActionResult SimplifiedRules() {
        return File(
            System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/rules_simple.pdf"),
            "application/pdf"
        );
    }

    [HttpGet("code_of_conduct")]
    public IActionResult CodeOfConduct() {
        return File(
            System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/code_of_conduct.pdf"),
            "application/pdf"
        );
    }

    [HttpGet("tournament_regulations")]
    public IActionResult TournamentRegulations() {
        return File(
            System.IO.File.OpenRead(
                Config.RESOURCES_FOLDER + "/documents/pdf/tournament_regulations.pdf"
            ),
            "application/pdf"
        );
    }
}