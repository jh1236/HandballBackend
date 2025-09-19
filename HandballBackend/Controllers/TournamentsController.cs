using HandballBackend.Authentication;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class TournamentsController : ControllerBase {
    public record GetTournamentsResponse {
        public required TournamentData[] Tournaments { get; set; }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetTournamentsResponse>> GetManyTournaments() {
        var db = new HandballContext();
        var tournaments = await db.Tournaments
            .OrderBy(t => t.Id)
            .Select(t => t.ToSendableData())
            .ToArrayAsync();
        return new GetTournamentsResponse {
            Tournaments = tournaments
        };
    }

    public record GetTournamentResponse {
        public required TournamentData Tournament { get; set; }
    }

    [HttpGet("{searchable}")]
    public async Task<ActionResult<GetTournamentResponse>> GetOneTournament(string searchable) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        return new GetTournamentResponse {
            Tournament = tournament.ToSendableData()
        };
    }


    [HttpPost("{searchable}/start")]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    public async Task<ActionResult> StartTournament(string searchable) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        tournament.BeginTournament();
        return Ok();
    }

    [HttpPost("{searchable}/finalsNextRound")]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    public async Task<ActionResult> PutTournamentInFinals(string searchable) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        tournament.InFinals = true;
        await db.SaveChangesAsync();
        return Ok();
    }


    public class AddOfficialRequest {
        public required string OfficialSearchableName { get; set; }
    }

    [HttpPost("{searchable}/addOfficial")]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    public async Task<ActionResult> AddOfficialToTournament(string searchable,
        [FromBody] AddOfficialRequest request) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var official = await db.Officials.IncludeRelevant().Include(official => official.TournamentOfficials)
            .FirstOrDefaultAsync(o => o.Person.SearchableName == request.OfficialSearchableName);


        if (official == null) {
            return BadRequest("The Official doesn't exist");
        }

        if (official.TournamentOfficials.Any(to => to.TournamentId == tournament.Id)) {
            return BadRequest("That official is already in this tournament!");
        }

        await db.TournamentOfficials.AddAsync(new TournamentOfficial {
            TournamentId = tournament.Id,
            OfficialId = official.Id,
            Role = OfficialRole.Umpire,
            UmpireProficiency = official.Proficiency,
            ScorerProficiency = official.Proficiency,
        });

        await db.SaveChangesAsync();
        return Ok();
    }

    public class RemoveOfficialRequest {
        public required string OfficialSearchableName { get; set; }
    }

    [TournamentAuthorize(PermissionType.UmpireManager)]
    [HttpDelete("{searchable}/removeOfficial")]
    public async Task<ActionResult> RemoveOfficialFromTournament(string searchable,
        [FromBody] RemoveOfficialRequest request) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var tournamentOfficial = await db.TournamentOfficials.FirstOrDefaultAsync(to =>
            to.TournamentId == tournament.Id && to.Official.Person.SearchableName == request.OfficialSearchableName);


        if (tournamentOfficial == null) {
            return BadRequest("The Official doesn't exist");
        }

        db.TournamentOfficials.Remove(tournamentOfficial);

        await db.SaveChangesAsync();
        return Ok();
    }
}