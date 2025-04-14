using System.ComponentModel.DataAnnotations;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class OfficialsController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Dictionary<string, dynamic?>> GetOfficials(
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        OfficialData[]? officials;

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid Tournament");
        }

        if (tournament is not null) {
            officials = db.TournamentOfficials
                .Where(a => a.TournamentId == tournament.Id)
                .IncludeRelevant()
                .Select(to => to.Official.ToSendableData(tournament, false))
                .ToArray();
        } else {
            officials = db.Officials
                .IncludeRelevant()
                .Select(o => o.ToSendableData(null, false))
                .ToArray();
        }

        var output = Utilities.WrapInDictionary("officials", officials);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }

    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Dictionary<string, dynamic?>> GetSingleOfficial(
        string searchable,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        var official = db.Officials.Where(o => o.Person.SearchableName == searchable).IncludeRelevant()
            .Include(g => g.Games)
            .ThenInclude(g => g.Players)
            .FirstOrDefault();
        if (official is null) {
            return NotFound("Invalid Name");
        }

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("invalid Tournament");
        }


        var output = Utilities.WrapInDictionary("official", official.ToSendableData(tournament, true));
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }
}