using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase {
    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Dictionary<string, dynamic?>> GetSingle(
        string searchable,
        [FromQuery] bool formatData = false,
        [FromQuery(Name = "tournament")] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        var player = db.People
            .Where(t => t.SearchableName == searchable)
            .Include(t => t.PlayerGameStats)!
            .ThenInclude(pgs => pgs.Game)
            .Select(t => t.ToSendableData(tournament, true, null, formatData)).FirstOrDefault();
        if (player is null) {
            return NotFound();
        }

        var output = Utilities.WrapInDictionary("player", player);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Dictionary<string, dynamic?>> GetMulti(
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
                .ThenInclude(pgs => pgs.Game);
        }

        var output = Utilities.WrapInDictionary("players",
            query.OrderBy(p => p.SearchableName)
                .Select(t => t.ToSendableData(tournament, includeStats, teamObj, formatData)).ToArray());
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }


        return output;
    }

    [HttpGet("stats")]
    public ActionResult<Dictionary<string, dynamic?>> GetAverage(
        [FromQuery] bool formatData = false,
        [FromQuery] string? tournamentSearchable = null,
        [FromQuery] bool returnTournament = false,
        [FromQuery] int? gameNumber = null) {
        var db = new HandballContext();
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        List<Dictionary<string, dynamic?>> statsList = [];
        if (gameNumber is not null) {
            statsList = db.PlayerGameStats
                .Where(pgs => pgs.Game.GameNumber == gameNumber.Value)
                .Include(pgs => pgs.Game)
                .ToArray()
                .Select(pgs => pgs.ToSendableData(true).stats!).ToList();
        } else if (tournament is not null) {
            statsList = db.People
                .Where(p => p.PlayerGameStats!.Any(pgs => pgs.TournamentId == tournament.Id))
                .Include(p => p.PlayerGameStats!
                    .Where(
                        pgs => pgs.TournamentId == tournament.Id
                               && pgs.Team.NonCaptainId != null &&
                               pgs.Opponent.NonCaptainId != null
                    )
                )
                .ThenInclude(pgs => pgs.Game)
                .ToArray()
                .Select(p => p.ToSendableData(tournament, true).stats!).ToList();
        } else {
            statsList = db.People
                .Include(p => p.PlayerGameStats!
                    .Where(
                        pgs => pgs.Team.NonCaptainId != null &&
                               pgs.Opponent.NonCaptainId != null
                    )
                )
                .ThenInclude(pgs => pgs.Game)
                .ToArray()
                .Select(p => p.ToSendableData(null, true).stats!).ToList();
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

        var output = Utilities.WrapInDictionary("stats", ret);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }
}