using HandballBackend.Authentication;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController(IAuthorizationService authorizationService) : ControllerBase {
    public record GetPlayerResponse {
        public required PersonData Player { get; set; }
        public TournamentData? Tournament { get; set; }
    }


    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetPlayerResponse>> GetSingle(
        string searchable,
        [FromQuery] bool formatData = false,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        var isAdmin = (await authorizationService.AuthorizeAsync(HttpContext.User, Policies.IsUmpireManager)).Succeeded;

        var player = db.People
            .Where(t => t.SearchableName == searchable)
            .Include(t => t.PlayerGameStats)!
            .ThenInclude(pgs => pgs.Game)
            .Select(t => t.ToSendableData(tournament, true, null, formatData, isAdmin)).FirstOrDefault();
        if (player is null) {
            return NotFound();
        }

        if (returnTournament && tournament is null) {
            return BadRequest("Cannot return null tournament");
        }


        return new GetPlayerResponse {
            Player = player,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }

    public record GetPlayersResponse {
        public required PersonData[] Players { get; set; }
        public TournamentData? Tournament { get; set; }
    }


    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetPlayersResponse>> GetMulti(
        [FromQuery] bool formatData = false,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] string? team = null,
        [FromQuery] bool returnTournament = false,
        [FromQuery] bool includeStats = false
    ) {
        var db = new HandballContext();
        IQueryable<Person> query;
        Team? teamObj = null;

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        if (team is not null) {
            teamObj = db.Teams.FirstOrDefault(t => t.SearchableName == team);
            if (teamObj is null) {
                return BadRequest("Invalid team");
            }
        }

        if (tournament is not null) {
            query = db.People.Where(p => p.PlayerGameStats!.Any(pgs => pgs.TournamentId == tournament.Id))
                .Include(p => p.PlayerGameStats)!
                .ThenInclude(pgs => pgs.Game);
        } else {
            query = db.People
                .Include(t => t.PlayerGameStats)!
                .ThenInclude(pgs => pgs.Game)
                .Where(p => p.SearchableName != "worstie");
        }

        var isAdmin = (await authorizationService.AuthorizeAsync(HttpContext.User, Policies.IsUmpireManager)).Succeeded;

        var playerSendable = query.OrderBy(p => p.SearchableName)
            .Where(p => p.PlayerGameStats!.Any(pgs =>
                !includeStats || tournament == null || pgs.TournamentId == tournament.Id))
            .Select(t => t.ToSendableData(tournament, includeStats, teamObj, formatData, isAdmin)).ToArray();

        if (returnTournament && tournament is null) {
            return BadRequest("Cannot return null tournament");
        }


        return new GetPlayersResponse {
            Players = playerSendable,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }

    public record GetStatsResponse {
        public Dictionary<string, dynamic?>? Stats { get; set; }
        public TournamentData? Tournament { get; set; }
    }

    [HttpGet("stats")]
    public ActionResult<GetStatsResponse> GetAverage(
        [FromQuery] bool formatData = false,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false,
        [FromQuery] int? gameNumber = null) {
        var db = new HandballContext();
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        List<Dictionary<string, dynamic?>> statsList;
        if (gameNumber is not null) {
            statsList = db.PlayerGameStats
                .Where(pgs => pgs.Game.GameNumber == gameNumber.Value)
                .Include(pgs => pgs.Game)
                .ToArray()
                .Select(pgs => pgs.ToSendableData(true).Stats!).ToList();
        } else if (tournament is not null) {
            statsList = db.People
                .Where(p => p.PlayerGameStats!.Any(pgs => pgs.TournamentId == tournament.Id))
                .Include(p => p.PlayerGameStats!
                    .Where(pgs => pgs.TournamentId == tournament.Id
                                  && pgs.Team.NonCaptainId != null &&
                                  pgs.Opponent.NonCaptainId != null
                    )
                )
                .ThenInclude(pgs => pgs.Game)
                .ToArray()
                .Select(p => p.ToSendableData(tournament, true).Stats!).ToList();
        } else {
            statsList = db.People
                .Include(p => p.PlayerGameStats!
                    .Where(pgs => pgs.Team.NonCaptainId != null &&
                                  pgs.Opponent.NonCaptainId != null
                    )
                )
                .ThenInclude(pgs => pgs.Game)
                .ToArray()
                .Select(p => p.ToSendableData(null, true).Stats!).ToList();
        }

        var ret = new Dictionary<string, dynamic?>();
        var counts = new Dictionary<string, double>();
        foreach (var stats in statsList) {
            if (stats.GetValueOrDefault("Games Played", 1) == 0) continue;
            foreach (var (k, v) in stats) {
                if (v is string) continue;
                if (v is double && (double.IsNaN(v) || double.IsInfinity(v))) continue;
                if (!ret.ContainsKey(k)) {
                    ret[k] = v;
                    counts[k] = 1;
                } else {
                    ret[k] += v;
                    counts[k] += 1;
                }
            }
        }

        if (formatData) {
            foreach (var stat in ret.Keys) {
                if (ret[stat] == null) {
                    ret[stat] = "-";
                    continue;
                }

                if (PersonData.PercentageColumns.Contains(stat)) {
                    ret[stat] = (ret[stat] / counts[stat]).ToString("P2");
                } else {
                    ret[stat] = Math.Round((ret[stat] / counts[stat]), 2);
                }
            }
        }

        if (returnTournament && tournament is null) {
            return BadRequest("Cannot return null tournament");
        }


        return new GetStatsResponse {
            Stats = ret,
            Tournament = returnTournament ? tournament!.ToSendableData() : null
        };
    }
}