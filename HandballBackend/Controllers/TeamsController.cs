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
    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Dictionary<string, dynamic?>> GetSingle(
        string searchable,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        TeamData teamData = null;
        if (tournament == null) {
            var team = db.Teams
                .Where(t => t.SearchableName == searchable)
                .IncludeRelevant()
                .Include(t => t.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .FirstOrDefault();
            if (team is null) {
                return NotFound();
            }

            teamData = team.ToSendableData(true, true, formatData);
        } else {
            var team = db.TournamentTeams
                .Where(t => t.Team.SearchableName == searchable)
                .IncludeRelevant()
                .Include(t => t.Team.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .FirstOrDefault();
            if (team is null) {
                return NotFound();
            }

            teamData = team.ToSendableData(true, true, formatData);
        }


        foreach (var (key, value) in teamData.stats) {
            Console.WriteLine($"{key}: {value}");
        }

        var output = Utilities.WrapInDictionary("team", teamData);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }

    [HttpGet]
    public ActionResult<Dictionary<string, dynamic?>> GetMultiple(
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

        List<TeamData> teamData = null;
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

            teamData = query.OrderBy(t => !EF.Functions.Like(t.Team.ImageUrl, "/api/%"))
                .ThenBy(t => EF.Functions.Like(t.Team.SearchableName, "solo_%"))
                .ThenBy(t => t.Team.SearchableName)
                .Select(t => t.ToSendableData(includeStats, includePlayerStats, formatData)).ToList();
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

            teamData = query.OrderBy(t => !EF.Functions.Like(t.ImageUrl, "/api/%"))
                .ThenBy(t => EF.Functions.Like(t.SearchableName, "solo_%"))
                .ThenBy(t => t.SearchableName)
                .Select(t => t.ToSendableData(includeStats, includePlayerStats, formatData, null)).ToList();
        }


        var output = Utilities.WrapInDictionary("teams", teamData);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }

    // TODO: Fix up for pooled tournaments
    [HttpGet("ladder")]
    public ActionResult<Dictionary<string, dynamic?>> GetLadder(
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        TeamData[]? ladder = null;
        TeamData[]? poolOne = null;
        TeamData[]? poolTwo = null;
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        if (tournament is not null) {
            (ladder, poolOne, poolTwo) = LadderHelper.GetTournamentLadder(db, tournament);
        } else {
            //Not null captain removes bye team
            var query = db.Teams.IncludeRelevant()
                .Include(t => t.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game)
                .Where(t => t.Captain != null);

            ladder = LadderHelper.SortTeamsNoTournament(query.Select(t => t.ToSendableData(true, false, false, null)).ToArray());
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

        var output = Utilities.WrapInDictionary("ladder", ladder);
        output["poolOne"] = poolOne;
        output["poolTwo"] = poolTwo;
        output["pooled"] = poolTwo is not null;
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }


        return output;
    }
}