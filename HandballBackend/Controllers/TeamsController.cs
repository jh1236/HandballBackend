using HandballBackend.Utils;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
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
            return BadRequest("Invalid tournament");
        }

        TeamData teamData;
        if (tournament == null) {
            var team = await db.Teams
                .Where(t => t.SearchableName == searchable)
                .IncludeRelevant()
                .Include(t => t.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .FirstOrDefaultAsync();
            if (team is null) {
                return NotFound();
            }

            teamData = team.ToSendableData(true, true, formatData);
        } else {
            var team = await db.TournamentTeams
                .Where(t => t.Team.SearchableName == searchable && t.TournamentId == tournament.Id)
                .IncludeRelevant()
                .Include(t => t.Team.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .FirstOrDefaultAsync();
            if (team is null) {
                return NotFound();
            }

            teamData = team.ToSendableData(true, true, formatData);
        }

        if (returnTournament && tournament is null) {
            return BadRequest("Cannot return null tournament");
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
            return BadRequest("Invalid tournament");
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
                        t.Team.Captain != null && p == t.Team.Captain.SearchableName ||
                        t.Team.NonCaptain != null && p == t.Team.NonCaptain.SearchableName ||
                        t.Team.Substitute != null && p == t.Team.Substitute.SearchableName
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
            return BadRequest("Cannot return null tournament");
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
            return BadRequest("Invalid tournament");
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
            var query = await db.Teams.IncludeRelevant()
                .Include(t => t.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .Where(t => t.Captain != null
                            && t.Captain.SearchableName != "worstie"
                            && (t.NonCaptain == null || t.NonCaptain.SearchableName != "worstie")
                            && (t.Substitute == null || t.Substitute.SearchableName != "worstie"))
                .Where(t => t.TournamentTeams.Any(tt => tt.TournamentId != 1)).ToArrayAsync();
            ladder = LadderHelper.SortTeamsNoTournament(query.Select(t => t.ToSendableData(true, false, false, null))
                .ToArray());
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
            return BadRequest("Cannot return null tournament");
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