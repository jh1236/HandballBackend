using System.ComponentModel.DataAnnotations;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.ErrorTypes;
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
    public async Task<ActionResult<GetOfficialsResponse>> GetManyOfficials(
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        OfficialData[]? officials;

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return NotFound(new InvalidTournament(tournamentSearchable));
        }

        if (tournament is not null) {
            var intermediate = await db.TournamentOfficials
                .Where(a => a.TournamentId == tournament.Id)
                .IncludeRelevant()
                .OrderBy(p => p.Official.Person.SearchableName)
                .ToArrayAsync();
            officials = intermediate.Select(to => to.Official.ToSendableData(tournament))
                .OrderByDescending(o => o.Role).ToArray();
            ;
        } else {
            officials = await db.Officials
                .IncludeRelevant()
                .OrderBy(p => p.Person.SearchableName)
                .Select(o => o.ToSendableData(null, false))
                .ToArrayAsync();
        }

        if (returnTournament && tournament is null) {
            return BadRequest(new TournamentNotProvidedForReturn());
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
    public async Task<ActionResult<GetOfficialResponse>> GetOneOfficial(
        string searchable,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        var official = await db.Officials.Where(o => o.Person.SearchableName == searchable).IncludeRelevant()
            .Include(o => o.Games)
            .ThenInclude(g => g.Players)
            .FirstOrDefaultAsync();
        if (official is null) {
            return NotFound(new DoesNotExist(nameof(official), searchable));
        }

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return NotFound(new InvalidTournament(tournamentSearchable));
        }


        if (returnTournament && tournament is null) {
            return BadRequest(new TournamentNotProvidedForReturn());
        }


        return new GetOfficialResponse {
            Official = official.ToSendableData(tournament, true),
            tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }
}