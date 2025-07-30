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
    public record GetTournamentsRepsonse {
        public required TournamentData[] Tournaments { get; set; }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetTournamentsRepsonse>> GetManyTournaments() {
        var db = new HandballContext();
        var tournaments = await db.Tournaments
            .OrderBy(t => t.Id)
            .Select(t => t.ToSendableData())
            .ToArrayAsync();
        return new GetTournamentsRepsonse {
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


    public class AddTeamRequest {
        public string? TeamName { get; set; }
        public string? CaptainName { get; set; }
        public string? NonCaptainName { get; set; }
        public string? SubstituteName { get; set; }
    }

    public class AddTeamResponse {
        public required TeamData Team { get; set; }
    }

    [HttpPost("{searchable}/addTeam")]
    [Authorize(Policy = Policies.IsAdmin)]
    public async Task<ActionResult<AddTeamResponse>> AddTeamToTournament([FromRoute] string searchable,
        [FromBody] AddTeamRequest request) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var team = await db.Teams.IncludeRelevant().Include(team => team.TournamentTeams)
            .FirstOrDefaultAsync(t => t.Name == request.TeamName);
        if (team is not null && (request.CaptainName is not null || request.NonCaptainName is not null ||
                                 request.SubstituteName is not null)) {
            return BadRequest("This Team already exists; you may not provide players");
        }


        if (team is null) {
            var playerIds = ((string?[]) [request.CaptainName, request.NonCaptainName, request.SubstituteName])
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(a => db.People.FirstOrDefault(p => p.Name == a)?.Id)
                .ToList();
            while (playerIds.Count < 3) {
                playerIds.Add(null);
            }

            var maybeTeam = db.Teams.IncludeRelevant().Include(t => t.TournamentTeams).FirstOrDefault(t =>
                // Both players must be in one of the roles
                (playerIds.Contains(t.CaptainId ?? null) &&
                 playerIds.Contains(t.NonCaptainId ?? null) &&
                 playerIds.Contains(t.SubstituteId ?? null)) &&

                // Count of non-null player references should be exactly 2
                ((t.CaptainId.HasValue ? 1 : 0) +
                    (t.NonCaptainId.HasValue ? 1 : 0) +
                    (t.SubstituteId.HasValue ? 1 : 0) == playerIds.Count(a => a.HasValue))
            );
            if (maybeTeam == null) {
                team = new Team {
                    CaptainId = playerIds![0],
                    NonCaptainId = playerIds[1],
                    SubstituteId = playerIds[2],
                    Name = request.TeamName!,
                    SearchableName = Utilities.ToSearchable(request.TeamName!)
                };
                await db.Teams.AddAsync(team);
                await db.SaveChangesAsync();
            } else {
                team = maybeTeam;
            }
        }

        if (team.TournamentTeams.Any(tt => tt.TournamentId == tournament.Id)) {
            return BadRequest("That team is already in this tournament!");
        }

        await db.TournamentTeams.AddAsync(new TournamentTeam {
            TournamentId = tournament.Id,
            TeamId = team.Id,
            Name = request.TeamName == null || request.TeamName == team.Name ? null : request.TeamName,
        });


        await db.SaveChangesAsync();
        return Ok(new AddTeamResponse {
            Team = team.ToSendableData()
        });
    }


    public class UpdateTeamRequest {
        public required string TeamSearchableName { get; set; }
        public string? NewName { get; set; }
        public string? NewColor { get; set; }
    }

    public class UpdateTeamResponse {
        public required TeamData Team { get; set; }
    }

    [HttpPatch("{searchable}/updateTeam")]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    public async Task<ActionResult<UpdateTeamResponse>> UpdateTeamForTournament(string searchable,
        [FromBody] UpdateTeamRequest request) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var team = await db.Teams.IncludeRelevant().Include(team => team.TournamentTeams)
            .SingleAsync(team => team.SearchableName == request.TeamSearchableName);

        if (team.TournamentTeams.All(tt => tt.TournamentId != tournament.Id)) {
            return BadRequest("Team not in tournament!");
        }

        var tournamentTeam = team.TournamentTeams.Single(tt => tt.TournamentId == tournament.Id);
        if (team.TournamentTeams.Count(tt => tt.Id != 1) == 1) {
            if (request.NewName != null) {
                team.Name = request.NewName;
                team.SearchableName = Utilities.ToSearchable(request.NewName);
            }

            if (request.NewColor != null) {
                team.TeamColor = request.NewColor;
            }
        } else {
            if (request.NewName != null) {
                tournamentTeam.Name = request.NewName;
            }

            if (request.NewColor != null) {
                tournamentTeam.TeamColor = request.NewColor;
            }
        }


        await db.SaveChangesAsync();
        return Ok(new UpdateTeamResponse {
            Team = tournamentTeam.ToSendableData()
        });
    }

    public class RemoveTeamRequest {
        public string? TeamSearchableName { get; set; }
    }

    [HttpDelete("{searchable}/removeTeam")]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    public async Task<ActionResult> RemoveTeamFromTournament(string searchable, [FromBody] RemoveTeamRequest request) {
        var db = new HandballContext();
        var tournament = await db.Tournaments
            .FirstOrDefaultAsync(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var team = await db.Teams.Include(team => team.TournamentTeams)
            .SingleAsync(t => t.SearchableName == request.TeamSearchableName);
        var deleteTeam = team.TournamentTeams.Count(tt => tt.TournamentId != 1) <= 1;

        var tournamentTeam = team.TournamentTeams.Single(tt => tt.TournamentId == tournament.Id);
        db.TournamentTeams.Remove(tournamentTeam);
        if (deleteTeam) {
            db.Teams.Remove(team);
        }

        db.TournamentTeams.Remove(tournamentTeam);
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