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
        [FromQuery] string? tournament = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("Invalid tournament");
        }

        var team = db.Teams
            .Where(t => t.SearchableName == searchable)
            .IncludeRelevant()
            .Include(t => t.PlayerGameStats)
            .ThenInclude(pgs => pgs.Game)
            .FirstOrDefault();
        if (team is null) {
            return NotFound();
        }

        var teamData = team.ToSendableData(tourney, true, true, formatData);

        foreach (var (key, value) in teamData.stats) {
            Console.WriteLine($"{key}: {value}");
        }

        var output = Utilities.WrapInDictionary("team", teamData);
        if (returnTournament && tourney is not null) {
            output["tournament"] = tourney.ToSendableData();
        }

        return output;
    }

    [HttpGet]
    public ActionResult<Dictionary<string, dynamic?>> GetMultiple(
        [FromQuery] string? tournament = null,
        [FromQuery] List<string>? players = null,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool includePlayerStats = false,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        IQueryable<Team> query;
        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("Invalid tournament");
        }

        if (tourney is not null) {
            IQueryable<TournamentTeam> innerQuery = db.TournamentTeams
                .Where(t => t.TournamentId == tourney.Id)
                .Include(t => t.Team.Captain)
                .Include(t => t.Team.NonCaptain)
                .Include(t => t.Team.Substitute);
            if (includeStats) {
                innerQuery = innerQuery
                    .Include(t => t.Team.PlayerGameStats)
                    .ThenInclude(pgs => pgs.Game);
            }

            query = innerQuery.Select(t => t.Team);
        } else {
            //Not null captain removes bye team
            query = db.Teams.IncludeRelevant();

            if (includeStats) {
                query = query
                    .Include(t => t.PlayerGameStats)
                    .ThenInclude(pgs => pgs.Game);
            }

            query = query.Where(t => t.Captain != null);
        }

        if (players != null) {
            foreach (var player in players) {
                query = query.Where(t =>
                    t.Captain != null && player == t.Captain.SearchableName ||
                    t.NonCaptain != null && player == t.NonCaptain.SearchableName ||
                    t.Substitute != null && player == t.Substitute.SearchableName
                );
            }
        }

        var teams = query.OrderBy(t => t.SearchableName)
            .Select(t => t.ToSendableData(tourney, includeStats, includePlayerStats, formatData)).ToArray();
        var output = Utilities.WrapInDictionary("teams", teams);
        if (returnTournament && tourney is not null) {
            output["tournament"] = tourney.ToSendableData();
        }

        return output;
    }

    // TODO: Fix up for pooled tournaments
    [HttpGet("ladder")]
    public ActionResult<Dictionary<string, dynamic?>> GetLadder(
        [FromQuery] string? tournament = null,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();

        IQueryable<Team> query;
        TeamData[]? ladder = null;
        TeamData[]? poolOne = null;
        TeamData[]? poolTwo = null;
        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("Invalid tournament");
        }

        if (tourney is not null) {
            (ladder, poolOne, poolTwo) = LadderHelper.SortLadder(db, tourney);
        } else {
            //Not null captain removes bye team
            query = db.Teams.IncludeRelevant()
                .Include(t => t.PlayerGameStats)
                .ThenInclude(pgs => pgs.Game);


            query = query.Where(t => t.Captain != null);

            ladder = LadderHelper.SortTeams(null, query.ToArray());
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
        if (returnTournament && tourney is not null) {
            output["tournament"] = tourney.ToSendableData();
        }

        return output;
    }
}