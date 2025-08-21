using HandballBackend.Utils;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.ErrorTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase {
    public record GetTeamResponse {
        public required TeamData Team { get; set; }
        public TournamentData? Tournament { get; set; }
    }

    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTeamResponse>> GetOneTeam(
        string searchable,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return NotFound(new InvalidTournament(tournamentSearchable));
        }

        TeamData? teamData;
        if (tournament == null) {
            teamData = await db.Teams
                .Where(t => t.SearchableName == searchable)
                .IncludeRelevant()
                .Include(t => t.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .Select(t => t.ToSendableData(true, true, formatData, null))
                .FirstOrDefaultAsync();
        } else {
            teamData = await db.TournamentTeams
                .Where(t => t.Team.SearchableName == searchable && t.TournamentId == tournament.Id)
                .IncludeRelevant()
                .Include(t => t.Team.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .Select(tt => tt.ToSendableData(true, true, formatData))
                .FirstOrDefaultAsync();
        }

        if (teamData is null) {
            return NotFound(new DoesNotExist("Team", searchable));
        }

        if (returnTournament && tournament is null) {
            return BadRequest(new TournamentNotProvidedForReturn());
        }


        return new GetTeamResponse {
            Team = teamData,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }

    public record GetTeamsResponse {
        public required TeamData[] Teams { get; set; }
        public TournamentData? Tournament { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<GetTeamsResponse>> GetManyTeams(
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] List<string>? player = null,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool includePlayerStats = false,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return NotFound(new InvalidTournament(tournamentSearchable));
        }

        TeamData[] teamData;
        if (tournament is not null) {
            IQueryable<TournamentTeam> query = db.TournamentTeams
                .Where(t => t.TournamentId == tournament.Id)
                .Include(t => t.Team.Captain)
                .Include(t => t.Team.NonCaptain)
                .Include(t => t.Team.Substitute);
            if (includeStats) {
                query = query
                    .Include(t => t.Team.PlayerGameStats)
                    .ThenInclude(pgs => pgs.Game);
            }


            if (player != null) {
                foreach (var p in player) {
                    query = query.Where(t =>
                        (t.Team.Captain != null && player.Contains(t.Team.Captain.SearchableName)) ||
                        (t.Team.NonCaptain != null && player.Contains(t.Team.NonCaptain.SearchableName)) ||
                        (t.Team.Substitute != null && player.Contains(t.Team.Substitute.SearchableName))
                    );
                }
            }

            teamData = await query.OrderBy(t => EF.Functions.Like(t.Team.SearchableName, "solo_%"))
                .ThenBy(t => !EF.Functions.Like(t.Team.ImageUrl, "/api/%"))
                .ThenBy(t => t.Team.SearchableName)
                .Select(t => t.ToSendableData(includeStats, includePlayerStats, formatData)).ToArrayAsync();
        } else {
            //Not null captain removes bye team
            var query = db.Teams.IncludeRelevant();

            if (includeStats) {
                query = query
                    .Include(t => t.PlayerGameStats)
                    .ThenInclude(pgs => pgs.Game);
            }

            query = query.Where(t => t.Captain != null);
            if (player != null) {
                foreach (var p in player) {
                    query = query.Where(t =>
                        t.Captain != null && p == t.Captain.SearchableName ||
                        t.NonCaptain != null && p == t.NonCaptain.SearchableName ||
                        t.Substitute != null && p == t.Substitute.SearchableName
                    );
                }
            }

            teamData = await query.OrderByDescending(t => t.TournamentTeams.Any(tt => tt.TournamentId != 1))
                .ThenBy(t => EF.Functions.Like(t.SearchableName, "solo_%"))
                .ThenBy(t => t.SearchableName)
                .Select(t => t.ToSendableData(includeStats, includePlayerStats, formatData, null)).ToArrayAsync();
        }

        if (returnTournament && tournament is null) {
            return BadRequest(new TournamentNotProvidedForReturn());
        }


        return new GetTeamsResponse {
            Teams = teamData,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }


    public record GetLadderResponse {
        public TeamData[]? Ladder { get; set; }
        public TeamData[]? PoolOne { get; set; }
        public TeamData[]? PoolTwo { get; set; }
        public bool Pooled { get; set; }
        public TournamentData? Tournament { get; set; }
    }

    [HttpGet("ladder")]
    public async Task<ActionResult<GetLadderResponse>> GetLadder(
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        TeamData[]? ladder;
        TeamData[]? poolOne = null;
        TeamData[]? poolTwo = null;
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return NotFound(new InvalidTournament(tournamentSearchable));
        }

        if (tournament is not null) {
            (ladder, poolOne, poolTwo) = await LadderHelper.GetTournamentLadder(db, tournament);
            if (tournament.Editable) {
                ladder = ladder?.Where(t => t.Stats!["Games Played"] > 0).ToArray();
                poolOne = poolOne?.Where(t => t.Stats!["Games Played"] > 0).ToArray();
                poolTwo = poolTwo?.Where(t => t.Stats!["Games Played"] > 0).ToArray();
            }
        } else {
            //Not null captain removes bye team
            ladder = await LadderHelper.GetLadder(db);
            ladder = ladder.Where(t => t.Stats!["Games Played"] > 0).ToArray();
        }


        if (formatData) {
            if (ladder is not null) {
                foreach (var team in ladder) {
                    team.FormatData();
                }
            }

            if (poolOne is not null) {
                foreach (var team in poolOne) {
                    team.FormatData();
                }
            }

            if (poolTwo is not null) {
                foreach (var team in poolTwo) {
                    team.FormatData();
                }
            }
        }

        if (returnTournament && tournament is null) {
            return BadRequest(new TournamentNotProvidedForReturn());
        }


        return new GetLadderResponse {
            Ladder = ladder,
            PoolOne = poolOne,
            PoolTwo = poolTwo,
            Pooled = poolOne is not null,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }
}