using HandballBackend.Database.SendableTypes;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TournamentController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<TournamentData> GetTournaments() {
        var db = new HandballContext();
        var tournaments = db.Tournaments
            .Select(a => a.ToSendableData())
            .ToArray();
        return tournaments;
    }

    [HttpGet("{searchable}")]
    public ActionResult<TournamentData> GetTournament(string searchable)
    {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null)
        {
            return NotFound("Invalid Tournament");
        }
        return tournament.ToSendableData();
    }
}