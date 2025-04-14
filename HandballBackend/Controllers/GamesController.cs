using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase {
    [HttpGet("change_code")]
    public ActionResult<Dictionary<string, dynamic?>> GetChangeCode(
        [FromQuery(Name = "id")] int gameNumber
    ) {
        var db = new HandballContext();
        var query = db.GameEvents.Where(gE => gE.Game.GameNumber == gameNumber).OrderByDescending(gE => gE.Id).First();

        return Utilities.WrapInDictionary("changeCode", query.Id);
    }

    [HttpGet("{gameNumber:int}")]
    public ActionResult<Dictionary<string, dynamic?>> GetSingleGame(
        int gameNumber,
        [FromQuery] bool includeGameEvents = false,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool formatData = false
    ) {
        var db = new HandballContext();
        var isAdmin = PermissionHelper.HasPermission(PermissionType.UmpireManager);


        var game = db.Games.IncludeRelevant()
            .Include(g => g.Events)
            .Include(g => g.Players)
            .ThenInclude(pgs => pgs.Player.Events.Where(e => GameEvent.CardTypes.Contains(e.EventType)))
            .ThenInclude(gE => gE.Game)
            .FirstOrDefault(g => g.GameNumber == gameNumber);
        if (game is null) {
            return NotFound();
        }

        return Utilities.WrapInDictionary("game",
            game.ToSendableData(true, includeGameEvents, includeStats, formatData, isAdmin));
    }

    [HttpGet]
    public ActionResult<Dictionary<string, dynamic?>> GetMulti(
        [FromQuery(Name = "tournament")] string? tournamentSearchable,
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

        var games = query.Select(g => g.ToSendableData(false, includeGameEvents, includeStats, formatData, isAdmin))
            .ToArray();

        var output = Utilities.WrapInDictionary("games", games);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }

    [HttpGet("noteable")]
    public ActionResult<Dictionary<string, dynamic?>> GetNoteable(
        [FromQuery(Name = "tournament")] string tournamentSearchable,
        [FromQuery] bool includeGameEvents = false,
        [FromQuery] bool returnTournament = false,
        [FromQuery] bool formatData = false,
        [FromQuery] bool includeStats = false,
        [FromQuery] int limit = -1
    ) {
        var db = new HandballContext();
        var isAdmin = PermissionHelper.HasPermission(PermissionType.UmpireManager);

        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        var query = db.Games.IncludeRelevant().Where(g => !g.IsBye && !Game.ResolvedStatuses.Contains(g.NoteableStatus));
        if (tournament is not null) {
            query = query.Where(g => g.TournamentId == tournament.Id);
        }


        query = query.OrderByDescending(g => g.Id);

        if (limit > 0) {
            query = query.Take(limit);
        }

        query = query.Include(g => g.Events);


        var games = query.Select(g => g.ToSendableData(false, includeGameEvents, includeStats, formatData, isAdmin))
            .ToArray();

        var output = Utilities.WrapInDictionary("games", games);
        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }


    [HttpGet("fixtures")]
    public ActionResult<Dictionary<string, dynamic?>> GetFixtures(
        [BindRequired, FromQuery(Name = "tournament")]
        string tournamentSearchable,
        [FromQuery] bool returnTournament = false,
        [FromQuery] bool seperateFinals = false,
        [FromQuery] int maxRounds = -1
    ) {
        var db = new HandballContext();
        var isAdmin = PermissionHelper.HasPermission(PermissionType.UmpireManager);
        Console.WriteLine(tournamentSearchable);
        if (!Utilities.TournamentOrElse(db, tournamentSearchable, out var tournament)) {
            return BadRequest("Invalid tournament");
        }


        var query = db.Games.Where(g => g.TournamentId == tournament.Id).IncludeRelevant();

        query = query.OrderBy(g => g.Id);


        var games = query.Select(g => g.ToSendableData(false, false, false, false, isAdmin)).ToArray();

        List<FixturesRound> fixtures = [];

        foreach (var game in games) {
            var roundIndex = game.round - 1;
            while (fixtures.Count < roundIndex) {
                // creates all before the current round
                fixtures.Add(new FixturesRound());
            }

            if (fixtures.Count == roundIndex) {
                //create the current round
                fixtures.Add(new FixturesRound([game], game.isFinal));
            } else {
                fixtures[game.round - 1].games.Add(game);
            }
        }

        foreach (var round in fixtures) {
            round.Sort();
        }

        if (maxRounds > 0) {
            fixtures = fixtures.TakeLast(maxRounds).ToList();
        }

        var output = Utilities.WrapInDictionary("fixtures", fixtures);

        if (seperateFinals) {
            output["fixtures"] = fixtures.Where(f => !f.final).ToArray();
            output["finals"] = fixtures.Where(f => f.final).ToArray();
        }

        if (returnTournament) {
            if (tournament is null) {
                return BadRequest("Cannot return null tournament");
            }

            output["tournament"] = tournament.ToSendableData();
        }

        return output;
    }
}

internal class FixturesRound(List<GameData>? games = null, bool isFinal = false) {
    public List<GameData> games { get; set; } = games == null ? [] : games;
    public bool final { get; set; } = isFinal;

    public void Sort() {
        games = FixturesHelper.SortFixtures(games);
    }
}