using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase {
    // GET api/values
    [HttpGet("rules")]
    public IActionResult GetRulesFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/rules.pdf"), "application/pdf");
    }
    [HttpGet("simplified_rules")]
    public IActionResult GetSimplifiedRulesFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/rules_simple.pdf"), "application/pdf");
    }
    [HttpGet("code_of_conduct")]
    public IActionResult GetCodeOfConductFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/code_of_conduct.pdf"), "application/pdf");
    }
    [HttpGet("tournament_regulations")]
    public IActionResult GetTournamentRegulationsFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/tournament_regulations.pdf"), "application/pdf");
    }
}