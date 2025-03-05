using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Models;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase {
    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Dictionary<string, dynamic>> GetSingle(
        string searchable,
        [FromQuery] bool formatData = true,
        [FromQuery] string? tournament = null,
        [FromQuery] bool returnTournament = true
    ) {
        var db = new HandballContext();
        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("Invalid tournament");
        }

        var player = db.People
            .Where(t => t.SearchableName == searchable)
            .Include(t => t.PlayerGameStats)!
            .ThenInclude(pgs => pgs.Game)
            .Select(t => t.ToSendableData(tourney, true, null, formatData)).FirstOrDefault();
        if (player is null) {
            return NotFound();
        }

        var output = Utilities.WrapInDictionary("player", player);
        if (returnTournament && tourney is not null) {
            output["tournament"] = tourney.ToSendableData();
        }

        return output;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Dictionary<string, dynamic>> GetMulti(
        [FromQuery] bool formatData = true,
        [FromQuery] string? tournament = null,
        [FromQuery] string? team = null,
        [FromQuery] bool returnTournament = true,
        [FromQuery] bool includeStats = true
    ) {
        var db = new HandballContext();
        IQueryable<Person> query;
        Team? teamObj = null;

        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("Invalid tournament");
        }

        if (team is not null) {
            teamObj = db.Teams.FirstOrDefault(t => t.SearchableName == team);
            if (teamObj is null) {
                return BadRequest("Invalid team");
            }
        }

        if (tourney is not null) {
            query = db.PlayerGameStats.Where(pgs => pgs.TournamentId == tourney.Id)
                .Select(pgs => pgs.Player)
                .Distinct()
                .Include(p => p.PlayerGameStats)!
                .ThenInclude(pgs => pgs.Game);
        } else {
            query = db.People
                .Include(t => t.PlayerGameStats)!
                .ThenInclude(pgs => pgs.Game);
        }

        var output = Utilities.WrapInDictionary("players",
            query.Select(t => t.ToSendableData(tourney, includeStats, teamObj, formatData)).ToArray());
        if (returnTournament && tourney is not null) {
            output["tournament"] = tourney.ToSendableData();
        }

        return output;
    }
}