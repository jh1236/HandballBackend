using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers.GameManagement;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.EndpointHelpers;

public static class GameManager {
    private static readonly string[] SIDES = ["Left", "Right", "Substitute"];

    private static readonly string?[] VALID_SCORE_METHODS = [
        null,
        "Double Bounce",
        "Straight",
        "Out of Court",
        "Double Touch",
        "Grabs",
        "Illegal Body Part",
        "Obstruction"
    ];


    

    private static void AddPointToGame(HandballContext db, Game game, bool firstTeam, int? playerId,
        bool penalty = false, string? notes = null) {
        var teamId = firstTeam ? game.TeamOneId : game.TeamTwoId;
        var prevEvent = game.Events.MaxBy(gE => gE.Id)!;
        var player = game.Players.FirstOrDefault(pgs => pgs.PlayerId == playerId);
        var newEvent = new GameEvent {
            TeamId = teamId,
            PlayerId = playerId != null ? player!.PlayerId : null,
            EventType = GameEventType.Score,
            GameId = game.Id,
            TournamentId = game.TournamentId,
            Details = null,
            Notes = penalty ? "Penalty" : notes,
            TeamWhoServedId = prevEvent.TeamToServeId,
            PlayerWhoServedId = prevEvent.PlayerToServeId,
            SideServed = prevEvent.SideToServe,

            TeamToServeId = teamId,
            PlayerToServeId = prevEvent.PlayerToServeId,
            SideToServe = prevEvent.SideToServe,
            TeamOneLeftId = prevEvent.TeamOneLeftId,
            TeamOneRightId = prevEvent.TeamOneRightId,
            TeamTwoLeftId = prevEvent.TeamTwoLeftId,
            TeamTwoRightId = prevEvent.TeamTwoRightId,
        };
        if (teamId == prevEvent.TeamToServeId) {
            //We won this point and the last point
            if (game.Tournament.BadmintonServes) {
                newEvent.SideToServe = prevEvent.SideToServe == "Left" ? "Right" : "Left";
                foreach (var pgs in game.Players.Where(pgs => pgs.TeamId == teamId)) {
                    pgs.SideOfCourt = pgs.SideOfCourt switch {
                        "Left" => "Right",
                        "Right" => "Left",
                        _ => pgs.SideOfCourt
                    };
                }

                if (firstTeam) {
                    newEvent.TeamOneLeftId = prevEvent.TeamOneRightId;
                    newEvent.TeamOneRightId = prevEvent.TeamOneLeftId;
                } else {
                    newEvent.TeamTwoLeftId = prevEvent.TeamTwoRightId;
                    newEvent.TeamTwoRightId = prevEvent.TeamTwoLeftId;
                }
            } else {
                // we are not in badminton; so nothing needs to change
            }
        } else {
            // this is our first win of the service, so we need to make changes accordingl
            var lastService = game.Events.Where(gE => gE.TeamToServeId == teamId).MaxBy(gE => gE.Id);
            // default side is the only difference between badminton and normal serves; the second team starts on the 
            // right if using badminton.
            var defaultSide = game.Tournament.BadmintonServes ? "Left" : "Right";
            newEvent.SideToServe = (lastService?.SideToServe ?? defaultSide) == "Left" ? "Right" : "Left";
            newEvent.PlayerToServeId = game.Players
                .First(pgs => pgs.TeamId == teamId && pgs.SideOfCourt == newEvent.SideToServe).PlayerId;
        }

        db.Add(newEvent);
        GameEventSynchroniser.SyncScorePoint(game, newEvent);
        db.SaveChanges();
    }

    public static void StartGame(int gameNumber, bool swapService, string[]? playersTeamOne,
        string[]? playersTeamTwo,
        bool teamOneIsIGa, string? officialSearchable = null, string? scorerSearchable = null) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().Include(g => g.Events).Include(game => game.Players)
            .ThenInclude(pgs => pgs.Player)
            .FirstOrDefault(g => g.GameNumber == gameNumber);
        if (game == null) throw new ArgumentException("The game number provided doesn't exist");
        if (game.Started) throw new InvalidOperationException("The game has already begun");
        List<PlayerGameStats> teamOneIds = [];
        List<PlayerGameStats> teamTwoIds = [];
        if (playersTeamOne == null) {
            teamOneIds = game.Players.Where(pgs => pgs.TeamId == game.TeamOneId).ToList();
        } else {
            foreach (var searchableName in playersTeamOne) {
                teamOneIds.Add(
                    game.Players.Single(pgs => pgs.Player.SearchableName == searchableName));
            }
        }

        if (playersTeamTwo == null) {
            teamTwoIds = game.Players.Where(pgs => pgs.TeamId == game.TeamOneId).ToList();
        } else {
            foreach (var searchableName in playersTeamTwo) {
                teamTwoIds.Add(
                    game.Players.Single(pgs => pgs.Player.SearchableName == searchableName));
            }
        }


        var igaId = teamOneIsIGa ? game.TeamOneId : game.TeamTwoId;
        game.Status = "In Progress";
        game.AdminStatus = "In Progress";
        game.NoteableStatus = "In Progress";
        game.IgaSideId = igaId;
        game.StartTime = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (officialSearchable != null) {
            var official = db.Officials.Where(o => o.Person.SearchableName == officialSearchable).Select(o => o.Id)
                .First();
            game.OfficialId = official;
        }

        if (scorerSearchable != null) {
            var scorer = db.Officials.Where(o => o.Person.SearchableName == scorerSearchable).Select(o => o.Id)
                .First();
            game.ScorerId = scorer;
        }

        foreach (var (pgs, side) in teamOneIds.Zip(SIDES)) {
            pgs.SideOfCourt = side;
            pgs.StartSide = side;
        }

        foreach (var (pgs, side) in teamTwoIds.Zip(SIDES)) {
            pgs.SideOfCourt = side;
            pgs.StartSide = side;
        }

        var servingTeamId = swapService ? game.TeamTwoId : game.TeamOneId;
        var servingPlayer = swapService ? teamOneIds[0] : teamTwoIds[0];
        teamOneIds.Add(teamOneIds.Last());
        teamTwoIds.Add(teamTwoIds.Last());

        var startEvent = new GameEvent {
            GameId = game.Id,
            TournamentId = game.TournamentId,
            EventType = GameEventType.Start,
            SideToServe = "Left",
            TeamToServeId = servingTeamId,
            PlayerToServeId = servingPlayer.PlayerId,
            TeamOneLeftId = teamOneIds[0].PlayerId,
            TeamOneRightId = teamOneIds[1].PlayerId,
            TeamTwoLeftId = teamTwoIds[0].PlayerId,
            TeamTwoRightId = teamTwoIds[1].PlayerId,
        };
        db.GameEvents.Add(startEvent);
        GameEventSynchroniser.SyncStartGame(game, startEvent);
        db.SaveChanges();
    }

    public static void ScorePoint(int gameNumber, bool firstTeam, bool leftPlayer, string? scoreMethod) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().Include(g => g.Events).First(g => g.GameNumber == gameNumber);
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        if (!VALID_SCORE_METHODS.Contains(scoreMethod)) {
            throw new ArgumentException("The score method provided is invalid");
        }

        int? player;
        var gameEvent = game.Events.MaxBy(gE => gE.Id)!;
        if (firstTeam) {
            player = leftPlayer ? gameEvent.TeamOneLeftId : gameEvent.TeamOneRightId;
        } else {
            player = leftPlayer ? gameEvent.TeamTwoLeftId : gameEvent.TeamTwoRightId;
        }

        AddPointToGame(db, game, firstTeam, player, notes: scoreMethod);
    }

    public static void ScorePoint(int gameNumber, bool firstTeam, string playerSearchable, string? scoreMethod) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().Include(g => g.Events).First(g => g.GameNumber == gameNumber);
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        if (!VALID_SCORE_METHODS.Contains(scoreMethod)) {
            throw new ArgumentException("The score method provided is invalid");
        }

        var player = game.Players.First(pgs => pgs.Player.SearchableName == playerSearchable);
        AddPointToGame(db, game, firstTeam, player.PlayerId, notes: scoreMethod);
    }
}