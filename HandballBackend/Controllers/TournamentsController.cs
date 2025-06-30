using HandballBackend.Authentication;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
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
    public ActionResult<GetTournamentsRepsonse> GetTournaments() {
        var db = new HandballContext();
        var tournaments = db.Tournaments
            .OrderBy(t => t.Id)
            .Select(t => t.ToSendableData())
            .ToArray();
        return new GetTournamentsRepsonse {
            Tournaments = tournaments
        };
    }

    public record GetTournamentResponse {
        public required TournamentData Tournament { get; set; }
    }

    [HttpGet("{searchable}")]
    public ActionResult<GetTournamentResponse> GetTournament(string searchable) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        return new GetTournamentResponse {
            Tournament = tournament.ToSendableData()
        };
    }


    [HttpGet("{searchable}/start")]
    [Authorize(Policy = Policies.IsAdmin)]
    public ActionResult StartTournament(string searchable) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        tournament.BeginTournament();
        return Ok();
    }


    public class AddTeamRequest {
        public string? TeamName { get; set; }
        public string? CaptainName { get; set; }
        public string? NonCaptainName { get; set; }
        public string? SubstituteName { get; set; }
    }

    [HttpPost("{searchable}/add_team")]
    [Authorize(Policy = Policies.IsAdmin)]
    public ActionResult AddTeamToTournament(string searchable, [FromBody] AddTeamRequest request) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var team = db.Teams.IncludeRelevant().Include(team => team.TournamentTeams)
            .FirstOrDefault(t => t.Name == request.TeamName);
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
                    SubstituteId = null,
                    Name = request.TeamName!,
                    SearchableName = Utilities.ToSearchable(request.TeamName!)
                };
                db.Teams.Add(team);
            } else {
                team = maybeTeam;
            }
        }

        if (team.TournamentTeams.Any(tt => tt.TournamentId == tournament.Id)) {
            return BadRequest("That team is already in this tournament!");
        }

        db.TournamentTeams.Add(new TournamentTeam {
            TournamentId = tournament.Id,
            TeamId = team.Id
        });


        db.SaveChanges();
        return Ok();
    }


    public class RemoveTeamRequest {
        public string? TeamSearchableName { get; set; }
    }

    [HttpDelete("{searchable}/remove_team")]
    [Authorize(Policy = Policies.IsAdmin)]
    public ActionResult RemoveTeamFromTournament(string searchable, [FromBody] RemoveTeamRequest request) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var tournamentTeam = db.TournamentTeams.FirstOrDefault(tt => tt.Team.SearchableName == request.TeamSearchableName);

        if (tournamentTeam is null) {
            return BadRequest("That team doesn't exist!");
        }

        db.TournamentTeams.Remove(tournamentTeam);


        db.SaveChanges();
        return Ok();
    }

    public class AddOfficialRequest {
        public required string OfficialSearchableName { get; set; }
    }

    [HttpPost("{searchable}/add_official")]
    [Authorize(Policy = Policies.IsAdmin)]
    public ActionResult AddOfficialToTournament(string searchable,
        [FromBody] AddOfficialRequest request) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var official = db.Officials.IncludeRelevant().Include(official => official.TournamentOfficials)
            .FirstOrDefault(o => o.Person.SearchableName == request.OfficialSearchableName);


        if (official == null) {
            return BadRequest("The Official doesn't exist");
        }

        if (official.TournamentOfficials.Any(to => to.TournamentId == tournament.Id)) {
            return BadRequest("That official is already in this tournament!");
        }

        db.TournamentOfficials.Add(new TournamentOfficial {
            TournamentId = tournament.Id,
            OfficialId = official.Id
        });

        db.SaveChanges();
        return Ok();
    }

    public class RemoveOfficialRequest {
        public required string OfficialSearchableName { get; set; }
    }


    [HttpDelete("{searchable}/remove_official")]
    public ActionResult RemoveOfficialFromTournament(string searchable,
        [FromBody] RemoveOfficialRequest request) {
        var db = new HandballContext();
        var tournament = db.Tournaments
            .FirstOrDefault(a => a.SearchableName == searchable);
        if (tournament is null) {
            return NotFound("Invalid Tournament");
        }

        if (tournament.Started) {
            return NotFound("Tournament has already started!");
        }

        var tournamentOfficial = db.TournamentOfficials.FirstOrDefault(to =>
            to.TournamentId == tournament.Id && to.Official.Person.SearchableName == request.OfficialSearchableName);


        if (tournamentOfficial == null) {
            return BadRequest("The Official doesn't exist");
        }


        db.TournamentOfficials.Remove(tournamentOfficial);

        db.SaveChanges();
        return Ok();
    }
}