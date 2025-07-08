using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TournamentsController : ControllerBase {
    public record GetTournamentsRepsonse {
        public required TournamentData[] Tournaments { get; set; }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<GetTournamentsRepsonse> GetTournaments() {
        var db = new HandballContext();
        var tournaments = db
            .Tournaments.OrderBy(t => t.Id)
            .Select(t => t.ToSendableData())
            .ToArray();
        return new GetTournamentsRepsonse { Tournaments = tournaments };
    }

    public record GetTournamentResponse {
        public required TournamentData Tournament { get; set; }
    }

    [HttpGet("{searchable}")]
    public ActionResult<GetTournamentResponse> GetTournament(string searchable) {
        var db = new HandballContext();
        var tournament = db.Tournaments.FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        return new GetTournamentResponse { Tournament = tournament.ToSendableData() };
    }
}