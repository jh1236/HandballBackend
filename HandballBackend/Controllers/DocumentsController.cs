using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase {
    // GET api/values

    public class IndexResponse {
        public Dictionary<string, string> Documents = new();
    }
    
    [HttpGet("index")]
    public ActionResult<IndexResponse> GetDocuments() {
        return Ok(new IndexResponse());
    }
    
    [HttpGet("simplified_rules")]
    public IActionResult GetSimplifiedRulesFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/rules_simple.pdf"),
            "application/pdf");
    }

    [HttpGet("code_of_conduct")]
    public IActionResult GetCodeOfConductFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/code_of_conduct.pdf"),
            "application/pdf");
    }

    [HttpGet("tournament_regulations")]
    public IActionResult GetTournamentRegulationsFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/tournament_regulations.pdf"),
            "application/pdf");
    }

    [HttpGet("rules/current")]
    public IActionResult GetRulesFile() {
        return File(System.IO.File.OpenRead(Config.RESOURCES_FOLDER + "/documents/pdf/rules.pdf"), "application/pdf");
    }
    
    [HttpGet("rules/{tournament}")]
    public IActionResult GetOldRulesFile(string tournament) {
        var fileName = Uri.EscapeDataString(tournament);
        var path = Config.RESOURCES_FOLDER + "/documents/old/" + tournament + ".pdf";
        return File(System.IO.File.OpenRead(path + fileName + ".png"), "image/png");
    }

}