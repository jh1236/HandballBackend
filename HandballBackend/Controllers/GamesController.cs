using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase {
    [HttpGet("{gameNumber:int}")]
    public ActionResult<Dictionary<string, dynamic?>> GetSingleGame(
        int gameNumber,
        [FromQuery] bool includeGameEvents = false,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool formatData = false
    ) {
        var db = new HandballContext();
        var isAdmin = PermissionHelper.HasPermission(PermissionType.UmpireManager);
        var query = db.Games.IncludeRelevant();
        if (includeGameEvents) {
            query = query.Include(g => g.Events);
        }

        var game = query.FirstOrDefault(g => g.GameNumber == gameNumber);
        if (game is null) {
            return NotFound();
        }

        return Utilities.WrapInDictionary("game",
            game.ToSendableData(includeGameEvents, includeStats, formatData, isAdmin));
    }

    [HttpGet]
    public ActionResult<Dictionary<string, dynamic?>> GetMulti(
        [FromQuery(Name = "tournament")] string tournamentSearchable,
        [FromQuery] bool includeGameEvents = false,
        [FromQuery] bool includeByes = false,
        [FromQuery] bool returnTournament = false,
        [FromQuery] bool includeStats = false,
        [FromQuery(Name = "player")] List<string>? players = null,
        [FromQuery(Name = "team")] List<string>? teams = null,
        [FromQuery(Name = "official")] List<string>? officials = null,
        [FromQuery] int? court = null,
        [FromQuery] bool formatData = false,
        [FromQuery] int limit = -1
    ) {
        var db = new HandballContext();
        var isAdmin = PermissionHelper.HasPermission(PermissionType.UmpireManager);

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        var query = db.Games.IncludeRelevant();
        if (tournament is not null) {
            query = query.Where(g => g.TournamentId == tournament.Id);
        }

        if (!includeByes) {
            query = query.Where(g => !g.IsBye);
        }

        if (players is not null) {
            foreach (var p in players) {
                query = query.Where(g => g.Players.Select(pgs => pgs.Player.SearchableName).Contains(p));
            }
        }

        if (teams is not null) {
            foreach (var t in teams) {
                query = query.Where(g => g.TeamOne.SearchableName == t || g.TeamTwo.SearchableName == t);
            }
        }

        if (officials is not null) {
            foreach (var p in officials) {
                query = query.Where(g =>
                    (g.Official != null && g.Official.Person.SearchableName == p) ||
                    (g.Scorer != null && g.Scorer.Person.SearchableName == p));
            }
        }

        if (court is not null) {
            query = query.Where(g => g.Court == court);
        }

        query = query.OrderByDescending(g => g.Id);

        if (limit > 0) {
            query = query.Take(limit);
        }

        if (includeGameEvents) {
            query = query.Include(g => g.Events);
        }

        var games = query.Select(g => g.ToSendableData(includeGameEvents, includeStats, formatData, isAdmin)).ToArray();

        var output = Utilities.WrapInDictionary("games", games);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }
}