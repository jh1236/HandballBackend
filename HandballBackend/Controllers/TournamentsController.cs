using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TournamentsController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, dynamic?>> GetTournaments() {
        var db = new HandballContext();
        var tournaments = db.Tournaments
            .Select(a => a.ToSendableData())
            .ToArray();
        return Utilities.WrapInDictionary("tournaments", tournaments);
    }

    [HttpGet("{searchable}")]
    public ActionResult<Dictionary<string, dynamic?>> GetTournament(string searchable)
    {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null)
        {
            return NotFound("Invalid Tournament");
        }
        return Utilities.WrapInDictionary("tournament", tournament.ToSendableData());
    }
}