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


    public record GetOfficialsResponse {
        public required OfficialData[] Officials { get; set; }
        public TournamentData? Tournament { get; set; }
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GetOfficialsResponse> GetManyOfficials(
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
                .OrderBy(p => p.Official.Person.SearchableName)
                .Select(to => to.Official.ToSendableData(tournament, false))
                .ToArray();
        } else {
            officials = db.Officials
                .IncludeRelevant()
                .OrderBy(p => p.Person.SearchableName)
                .Select(o => o.ToSendableData(null, false))
                .ToArray();
        }

        if (returnTournament && tournament is null) {
            return BadRequest("Cannot return null tournament");
        }


        return new GetOfficialsResponse {
            Officials = officials,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }

    public record GetOfficialResponse {
        public required OfficialData Official { get; set; }
        public TournamentData? tournament { get; set; }
    }

    
    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GetOfficialResponse> GetOneOfficial(
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


        if (returnTournament && tournament is null) {
            return BadRequest("Cannot return null tournament");
        }


        return new GetOfficialResponse {
            Official = official.ToSendableData(tournament, true),
            tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }
}