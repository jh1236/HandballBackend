using HandballBackend.Controllers;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HandballBackend.EndpointHelpers.GameManagement;

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

    private static readonly GameEventType[] IGNORED_BY_UNDO = [
        GameEventType.Notes,
        GameEventType.EndTimeout,
        GameEventType.Protest,
        GameEventType.Resolve,
    ];


    private static void BroadcastUpdate(int gameId) {
        _ = ScoreboardController.SendGame(gameId);
    }

    private static GameEvent SetUpGameEvent(Game game, GameEventType type, bool? firstTeam, int? playerId,
        string? notes = null, int? details = null) {
        int? teamId = firstTeam != null ? (firstTeam.Value ? game.TeamOneId : game.TeamTwoId) : null;
        var prevEvent = game.Events.OrderByDescending(gE => gE.Id).FirstOrDefault()!;
        var player = game.Players.FirstOrDefault(pgs => pgs.PlayerId == playerId);
        var newEvent = new GameEvent {
            TeamId = teamId,
            PlayerId = playerId != null ? player!.PlayerId : null,
            EventType = type,
            GameId = game.Id,
            TournamentId = game.TournamentId,
            Details = details,
            Notes = notes,
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
        return newEvent;
    }


    private static void AddPointToGame(HandballContext db, int gameNumber, bool firstTeam, int? playerId,
        bool penalty = false, string? notes = null) {
        var game = db.Games.IncludeRelevant().Include(g => g.Events).SingleOrDefault(g => g.GameNumber == gameNumber);
        var teamId = firstTeam ? game.TeamOneId : game.TeamTwoId;
        var prevEvent = game.Events.OrderByDescending(gE => gE.Id).FirstOrDefault()!;
        var newEvent = SetUpGameEvent(game, GameEventType.Score, firstTeam, playerId, penalty ? "Penalty" : notes);
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
            // this is our first win of the service, so we need to make changes accordingly
            var lastService = game.Events.Where(gE => gE.TeamToServeId == teamId).OrderByDescending(gE => gE.Id)
                .FirstOrDefault();
            // default side is the only difference between badminton and normal serves; the second team starts on the 
            // right if using badminton.
            var defaultSide = game.Tournament!.BadmintonServes ? "Left" : "Right";
            newEvent.TeamToServeId = teamId;
            newEvent.SideToServe = (lastService?.SideToServe ?? defaultSide) == "Left" ? "Right" : "Left";
            var teamPlayers = game.Players!.Where(pgs => pgs.TeamId == teamId).ToList();
            if (teamPlayers.Count == 1) {
                newEvent.PlayerToServeId = teamPlayers[0].PlayerId;
            } else {
                newEvent.PlayerToServeId = teamPlayers
                    .First(pgs => pgs.SideOfCourt == newEvent.SideToServe).PlayerId;
            }
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
                    game.Players.First(pgs => pgs.Player.SearchableName == searchableName));
            }
        }

        if (playersTeamTwo == null) {
            teamTwoIds = game.Players.Where(pgs => pgs.TeamId == game.TeamOneId).ToList();
        } else {
            foreach (var searchableName in playersTeamTwo) {
                teamTwoIds.Add(
                    game.Players.First(pgs => pgs.Player.SearchableName == searchableName));
            }
        }


        var igaId = teamOneIsIGa ? game.TeamOneId : game.TeamTwoId;
        game.Status = "In Progress";
        game.AdminStatus = "In Progress";
        game.NoteableStatus = "In Progress";
        game.IgaSideId = igaId;
        game.StartTime = Utilities.GetUnixSeconds();
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
        var servingPlayer = swapService ? teamTwoIds[0] : teamOneIds[0];
        game.TeamToServeId = servingTeamId;
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
        BroadcastUpdate(gameNumber);
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
        var gameEvent = game.Events.OrderBy(gE => gE.Id).FirstOrDefault()!;
        if (firstTeam) {
            player = leftPlayer ? gameEvent.TeamOneLeftId : gameEvent.TeamOneRightId;
        } else {
            player = leftPlayer ? gameEvent.TeamTwoLeftId : gameEvent.TeamTwoRightId;
        }

        AddPointToGame(db, gameNumber, firstTeam, player, notes: scoreMethod);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void ScorePoint(int gameNumber, bool firstTeam, string playerSearchable, string? scoreMethod) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        if (!VALID_SCORE_METHODS.Contains(scoreMethod)) {
            throw new ArgumentException("The score method provided is invalid");
        }

        var player = game.Players.First(pgs => pgs.Player.SearchableName == playerSearchable);
        AddPointToGame(db, gameNumber, firstTeam, player.PlayerId, notes: scoreMethod);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Ace(int gameNumber) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var prevGameEvent = game.Events.OrderByDescending(gE => gE.Id).FirstOrDefault()!;
        var firstTeam = prevGameEvent.TeamToServeId == game.TeamOneId;
        AddPointToGame(db, gameNumber, firstTeam, prevGameEvent.PlayerToServeId, notes: "Ace");
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Fault(int gameNumber) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var lastGameEvent = game.Events.OrderByDescending(gE => gE.Id).FirstOrDefault()!;
        var firstTeam = lastGameEvent.TeamToServeId == game.TeamOneId;
        var gameEvent = SetUpGameEvent(game, GameEventType.Fault, firstTeam, lastGameEvent.PlayerToServeId);
        var faulted = game.Events.Where(gE => gE.EventType is GameEventType.Fault or GameEventType.Score)
            .OrderByDescending(gE => gE.Id)
            .Select(gE => gE.EventType is GameEventType.Fault).FirstOrDefault(false);
        db.Add(gameEvent);
        if (faulted) {
            AddPointToGame(db, gameNumber, !firstTeam, null, true);
        }

        GameEventSynchroniser.SyncFault(game, gameEvent);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Timeout(int gameNumber, bool firstTeam) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var gameEvent = SetUpGameEvent(game, GameEventType.Timeout, firstTeam, null);
        db.Add(gameEvent);
        GameEventSynchroniser.SyncTimeout(game, gameEvent);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Forfeit(int gameNumber, bool firstTeam) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var gameEvent = SetUpGameEvent(game, GameEventType.Forfeit, firstTeam, null);
        db.Add(gameEvent);
        GameEventSynchroniser.SyncForfeit(game, gameEvent);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void EndTimeout(int gameNumber) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var gameEvent = SetUpGameEvent(game, GameEventType.EndTimeout, null, null);
        db.Add(gameEvent);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Substitute(int gameNumber, bool firstTeam, string playerSearchable) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var gameEvent = SetUpGameEvent(game, GameEventType.Substitute, null, null);
        var teamId = firstTeam ? game.TeamOneId : game.TeamTwoId;
        var playerComingIn = game.Players.First(pgs => pgs.TeamId == teamId && pgs.SideOfCourt == "Substitute");
        var playerGoingOut =
            game.Players.First(pgs => pgs.TeamId == teamId && pgs.Player.SearchableName == playerSearchable);
        var leftSide = playerGoingOut.SideOfCourt == "Left";
        playerComingIn.SideOfCourt = playerGoingOut.SideOfCourt;
        playerGoingOut.SideOfCourt = "Substitute";
        if (firstTeam) {
            if (leftSide) {
                gameEvent.TeamOneLeftId = playerComingIn.PlayerId;
            } else {
                gameEvent.TeamOneRightId = playerComingIn.PlayerId;
            }
        } else {
            if (leftSide) {
                gameEvent.TeamTwoLeftId = playerComingIn.PlayerId;
            } else {
                gameEvent.TeamTwoRightId = playerComingIn.PlayerId;
            }
        }

        db.Add(gameEvent);
        // GameEventSynchroniser.SyncSubstitute(game, gameEvent);  //Doesn't exist
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Substitute(int gameNumber, bool firstTeam, bool leftPlayer) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var gameEvent = SetUpGameEvent(game, GameEventType.Substitute, null, null);
        var teamId = firstTeam ? game.TeamOneId : game.TeamTwoId;
        var playerComingIn = game.Players.First(pgs => pgs.TeamId == teamId && pgs.SideOfCourt == "Substitute");
        var prevEvent = game.Events.OrderBy(gE => gE.Id).FirstOrDefault()!;
        int playerGoingOutId;
        if (firstTeam) {
            playerGoingOutId = (leftPlayer ? prevEvent.TeamOneLeftId : prevEvent.TeamOneRightId)!.Value;
        } else {
            playerGoingOutId = (leftPlayer ? prevEvent.TeamTwoLeftId : prevEvent.TeamTwoRightId)!.Value;
        }

        var playerGoingOut = game.Players.First(p => p.PlayerId == playerGoingOutId);
        var leftSide = playerGoingOut.SideOfCourt == "Left";
        playerComingIn.SideOfCourt = playerGoingOut.SideOfCourt;
        playerGoingOut.SideOfCourt = "Substitute";
        if (firstTeam) {
            if (leftSide) {
                gameEvent.TeamOneLeftId = playerComingIn.PlayerId;
            } else {
                gameEvent.TeamOneRightId = playerComingIn.PlayerId;
            }
        } else {
            if (leftSide) {
                gameEvent.TeamTwoLeftId = playerComingIn.PlayerId;
            } else {
                gameEvent.TeamTwoRightId = playerComingIn.PlayerId;
            }
        }

        db.Add(gameEvent);
        // GameEventSynchroniser.SyncSubstitute(game, gameEvent);  //Doesn't exist
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Card(int gameNumber, bool firstTeam, string playerSearchable, string color, int duration,
        string reason) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();

        var player = game.Players.First(pgs => pgs.Player.SearchableName == playerSearchable).PlayerId;
        CardInternal(db, gameNumber, firstTeam, player, color, duration, reason);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Card(int gameNumber, bool firstTeam, bool leftPlayer, string color, int duration,
        string reason) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().Include(g => g.Events).FirstOrDefault(g => g.GameNumber == gameNumber);
        int player;
        var prevEvent = game.Events.OrderBy(gE => gE.Id).FirstOrDefault()!;
        if (firstTeam) {
            player = (leftPlayer ? prevEvent.TeamOneLeftId : prevEvent.TeamOneRightId)!.Value;
        } else {
            player = (leftPlayer ? prevEvent.TeamTwoLeftId : prevEvent.TeamTwoRightId)!.Value;
        }


        CardInternal(db, gameNumber, firstTeam, player, color, duration, reason);

        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    private static void CardInternal(HandballContext db, int gameId, bool firstTeam, int playerId, string color,
        int duration,
        string reason) {
        var game = db.Games.IncludeRelevant().Include(g => g.Events).FirstOrDefault(g => g.GameNumber == gameId);
        if (game == null) throw new ArgumentException("The game has not been found");
        if (color != "Warning" && !color.EndsWith(" Card")) {
            color += " Card";
        }

        var type = color switch {
            "Warning" => GameEventType.Warning,
            "Green Card" => GameEventType.GreenCard,
            "Yellow Card" => GameEventType.YellowCard,
            "Red Card" => GameEventType.RedCard,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };


        var gameEvent = SetUpGameEvent(game, type, firstTeam, playerId, reason, duration);
        db.Add(gameEvent);
        GameEventSynchroniser.SyncCard(game, gameEvent);


        var teamId = firstTeam ? game.TeamOneId : game.TeamTwoId;
        var players = game.Players.Where(pgs => pgs.SideOfCourt != "Substitute" && pgs.TeamId == teamId).ToList();

        var bothCarded = players
            .Select(i => i.CardTimeRemaining >= 0 ? i.CardTimeRemaining : 12)
            .DefaultIfEmpty(0)
            .Min();

        if (!game.SomeoneHasWon && (bothCarded != 0 || players.Count == 1)) {
            var myScore = game.TeamOneScore;
            var theirScore = game.TeamTwoScore;

            if (!firstTeam) {
                (myScore, theirScore) = (theirScore, myScore);
            }

            bothCarded = Math.Min(bothCarded, Math.Max(11 - theirScore, myScore + 2 - theirScore));

            for (var i = 0; i < (players.Count == 1 ? duration : bothCarded); i++) {
                AddPointToGame(
                    db,
                    gameId,
                    !firstTeam,
                    null,
                    penalty: true
                );
                foreach (var pgs in players.Where(pgs => pgs.CardTimeRemaining > 0)) {
                    pgs.CardTimeRemaining--;
                }
            }
        }
    }

    public static void Undo(int gameNumber) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().Include(g => g.Events).First(g => g.GameNumber == gameNumber);
        if (!game.Started) throw new InvalidOperationException("The game has not started");
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        var smallestId = game.Events.Where(gE => !IGNORED_BY_UNDO.Contains(gE.EventType) && gE.Notes != "Penalty")
            .OrderByDescending(gE => gE.Id).First().Id;
        db.GameEvents.Where(gE => gE.GameId == game.Id && gE.Id >= smallestId).ExecuteDelete();
        db.SaveChanges(); // Not necessary but probably still a good idea


        db = new HandballContext();
        GameEventSynchroniser.SyncGame(db, gameNumber);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void Delete(int gameNumber) {
        var db = new HandballContext();
        var game = db.Games.Include(game => game.Tournament).FirstOrDefault(g => g.GameNumber == gameNumber);
        if (!game.Tournament.Editable) {
            throw new InvalidOperationException("The game is not in an editable tournament");
        }

        db.GameEvents.Where(gE => gE.GameId == game.Id).ExecuteDelete();
        db.PlayerGameStats.Where(pgs => pgs.GameId == game.Id).ExecuteDelete();
        db.Remove(game);
        db.SaveChanges();
        BroadcastUpdate(gameNumber);
    }

    public static void End(
        int gameNumber,
        List<string> bestPlayerOrder,
        int teamOneRating, int teamTwoRating,
        string notes,
        string? protestReasonTeamOne, string? protestReasonTeamTwo,
        string notesTeamOne, string notesTeamTwo,
        bool markedForReview
    ) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.SomeoneHasWon) throw new InvalidOperationException("The game has not ended!");
        var playersInOrder = game.Players.OrderBy(p => bestPlayerOrder.IndexOf(p.Player.SearchableName)).ToList();
        var isForfeit = game.Events.Any(gE => gE.EventType == GameEventType.Forfeit);

        var endEvent = SetUpGameEvent(game, GameEventType.EndGame, null, null, notes);
        db.Add(endEvent);


        if (!string.IsNullOrEmpty(protestReasonTeamOne)) {
            var protestEvent = SetUpGameEvent(game, GameEventType.Protest, true, null, protestReasonTeamOne);
            db.Add(protestEvent);
        }

        if (!string.IsNullOrEmpty(protestReasonTeamTwo)) {
            var protestEvent = SetUpGameEvent(game, GameEventType.Protest, false, null, protestReasonTeamTwo);
            db.Add(protestEvent);
        }

        var notesTeamOneEvent = SetUpGameEvent(game, GameEventType.Notes, true, null, notesTeamOne, teamOneRating);
        db.Add(notesTeamOneEvent);
        var notesTeamTwoEvent = SetUpGameEvent(game, GameEventType.Notes, false, null, notesTeamTwo, teamTwoRating);
        db.Add(notesTeamTwoEvent);
        var votes = 2;
        foreach (var pgs in playersInOrder) {
            pgs.Rating = pgs.TeamId == game.TeamOneId ? teamOneRating : teamTwoRating;
            if (votes <= 0) continue;
            var votesEvent = SetUpGameEvent(game, GameEventType.Votes, true, pgs.PlayerId, details: votes);
            pgs.BestPlayerVotes = votes--;
            db.Add(votesEvent);
        }

        var protested = !string.IsNullOrEmpty(protestReasonTeamOne) || !string.IsNullOrEmpty(protestReasonTeamTwo);
        game.MarkedForReview = markedForReview;
        game.Protested = protested;

        if (game.Players.Any(i => i.RedCards != 0)) {
            game.AdminStatus = "Red Card Awarded";
        } else if (protested) {
            game.AdminStatus = "Protested";
        } else if (markedForReview) {
            game.AdminStatus = "Marked for Review";
        } else if (game.Players.Any(i => i.YellowCards != 0)) {
            game.AdminStatus = "Yellow Card Awarded";
        } else if (teamOneRating == 1 || teamTwoRating == 1) {
            game.AdminStatus = "Unsportsmanlike Conduct";
        } else if (isForfeit) {
            game.AdminStatus = "Forfeit";
        } else {
            game.AdminStatus = "Official";
        }


        game.WinningTeamId = game.TeamOneScore > game.TeamTwoScore ? game.TeamOneId : game.TeamTwoId;
        game.NoteableStatus = game.AdminStatus;
        game.Status = "Official";
        game.Ended = true;
        game.Length = Utilities.GetUnixSeconds() - game.StartTime;
        game.Notes = notes;
        if (game is {
                Ranked:
                true,
                IsFinal:
                false
            }) {
            var playingPlayers = game.Players
                .Where(pgs => (isForfeit || pgs.RoundsCarded + pgs.RoundsOnCourt > 0)).ToList();
            var teamOneElo = playingPlayers.Where(pgs => pgs.TeamId == game.TeamOneId).Select(pgs => pgs.InitialElo)
                .Average();
            var teamTwoElo = playingPlayers.Where(pgs => pgs.TeamId == game.TeamTwoId).Select(pgs => pgs.InitialElo)
                .Average();
            foreach (var pgs in game.Players) {
                var myElo = pgs.TeamId == game.TeamOneId ? teamOneElo : teamTwoElo;
                var oppElo = pgs.TeamId == game.TeamOneId ? teamTwoElo : teamOneElo;
                pgs.EloDelta = EloCalculator.CalculateEloDelta(myElo, oppElo, game.WinningTeamId == pgs.TeamId);
            }
        }

        db.SaveChanges();
        if (game.Tournament.TextAlerts) {
            var nextGame = db.Games
                .Where(g => g.TournamentId == game.TournamentId && !g.IsBye && !g.Ended && g.Id > game.Id &&
                            g.Court == game.Court)
                .IncludeRelevant()
                .OrderBy(g => g.Id).FirstOrDefault();
            if (nextGame != null) {
                TextHelper.TextPeopleForGame(nextGame);
            }
        }

        var remainingGames =
            db.Games.Any(g => g.TournamentId == game.TournamentId && !g.IsBye && !g.Ended && g.Id != game.Id);
        if (!remainingGames) {
            game.Tournament.EndRound();
        }
        BroadcastUpdate(gameNumber);
    }

    public static Game CreateGame(int tournamentId, string?[]? playersTeamOne, string?[]? playersTeamTwo,
        string? teamOneName, string? teamTwoName, int officialId = -1,
        int scorerId = -1, int round = -1, int court = 0, bool isFinal = false) {
        var db = new HandballContext();
        var teams = new List<Team>();
        foreach (var (players, teamName) in new[] {(playersTeamOne, teamOneName), (playersTeamTwo, teamTwoName)}) {
            Team team;
            if (players == null || players.Length == 0) {
                if (teamName == null) {
                    throw new ArgumentNullException(teams.Count == 0 ? nameof(playersTeamOne) : nameof(playersTeamTwo),
                        "You must specify either a team name or the players for the team");
                }

                team = db.Teams.IncludeRelevant().FirstOrDefault(t => t.Name == teamName)!;
            } else {
                var playerIds = players.Select(a => db.People.FirstOrDefault(p => p.Name == a)?.Id)
                    .ToList();
                while (playerIds.Count < 3) {
                    playerIds.Add(null);
                }

                var maybeTeam = db.Teams.IncludeRelevant().FirstOrDefault(t =>
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
                        Name = teamName,
                        SearchableName = Utilities.ToSearchable(teamName)
                    };
                    db.Teams.Add(team);
                } else {
                    team = maybeTeam;
                }
            }

            teams.Add(team);
        }

        db.SaveChanges();
        return CreateGame(tournamentId, teams[0].Id, teams[1].Id, officialId, scorerId, round, court, isFinal);
    }


    public static Game CreateGame(int tournamentId, int teamOneId, int teamTwoId,
        int officialId = -1,
        int scorerId = -1, int round = -1, int court = 0, bool isFinal = false) {
        var db = new HandballContext();
        var teamOne = db.Teams.Where(t => t.Id == teamOneId).IncludeRelevant().Single();
        var teamTwo = db.Teams.Where(t => t.Id == teamTwoId).IncludeRelevant().Single();
        var tournament = db.Tournaments.Find(tournamentId)!;
        var ranked = tournament.Ranked;
        var isBye = false;
        foreach (var team in new[] {teamOne, teamTwo}) {
            if (team.Id == 1) {
                // this is the bye team
                isBye = true;
                continue;
            }

            ranked &= team.CaptainId != null;
            var tt = db.TournamentTeams.FirstOrDefault(t => t.TournamentId == tournamentId && t.TeamId == team.Id);
            if (tt != null) continue;
            tt = new TournamentTeam {
                TeamId = team.Id,
                TournamentId = tournamentId
            };
            db.Add(tt);
        }

        if (round < 0) {
            var lastGame = db.Games.Where(g => g.StartTime != null && g.TournamentId == tournamentId)
                .OrderByDescending(g => g.GameNumber)
                .FirstOrDefault();
            int lastStartTime;
            if (lastGame == null) {
                lastStartTime = -1;
                round = 0;
            } else {
                lastStartTime = lastGame.StartTime!.Value;
                round = lastGame.Round;
            }

            if (Utilities.GetUnixSeconds() > lastStartTime + 32400) {
                round++;
            }
        }

        court = isBye ? -1 : court;
        if (isBye && teamOneId == 1) {
            teamOneId = teamTwoId;
            teamTwoId = 1;
        }

        var gameNumber =
            isBye ? -1 : (db.Games.OrderByDescending(g => g.GameNumber).FirstOrDefault()?.GameNumber ?? 0) + 1;
        var game = new Game {
            GameNumber = gameNumber,
            TournamentId = tournamentId,
            TeamOneId = teamOneId,
            TeamTwoId = teamTwoId,
            IgaSideId = teamOneId,
            OfficialId = officialId > 0 ? officialId : null,
            Court = court,
            IsFinal = isFinal,
            Round = round,
            Ranked = ranked,
            IsBye = isBye,
            SomeoneHasWon = isBye
        };
        if (isBye) {
            game.Status = "Bye";
            game.AdminStatus = "Bye";
            game.WinningTeamId = 1;
        }

        db.Add(game);
        db.SaveChanges();
        game = db.Games.Where(g => g.Id == game.Id)
            .Include(g =>
                g.TeamOne.Captain.PlayerGameStats.OrderByDescending(pgs => pgs.GameId).Take(1))
            .Include(g =>
                g.TeamOne.NonCaptain.PlayerGameStats.OrderByDescending(pgs => pgs.GameId).Take(1))
            .Include(g =>
                g.TeamOne.Substitute.PlayerGameStats.OrderByDescending(pgs => pgs.GameId).Take(1))
            .Include(g =>
                g.TeamTwo.Captain.PlayerGameStats.OrderByDescending(pgs => pgs.GameId).Take(1))
            .Include(g =>
                g.TeamTwo.NonCaptain.PlayerGameStats.OrderByDescending(pgs => pgs.GameId).Take(1))
            .Include(g =>
                g.TeamTwo.Substitute.PlayerGameStats.OrderByDescending(pgs => pgs.GameId).Take(1))
            .IncludeRelevant()
            .Single(); //used to pull extra gamey data


        foreach (var team in new[] {teamOne, teamTwo}) {
            if (team.Id == 1) continue;
            Person?[] teamPlayers = [team.Captain, team.NonCaptain, team.Substitute];
            foreach (var p in teamPlayers.Where(p => p != null)) {
                var prevGame = p!.PlayerGameStats!.OrderByDescending(pgs => pgs.GameId).FirstOrDefault();
                var carryCardTimes = game.TournamentId >= 7 && prevGame?.TournamentId == game.TournamentId;
                db.Add(new PlayerGameStats {
                    GameId = game.Id,
                    PlayerId = p.Id,
                    TournamentId = tournamentId,
                    TeamId = team.Id,
                    OpponentId = team.Id == teamOneId ? teamTwoId : teamOneId,
                    InitialElo = (prevGame?.InitialElo ?? 1500.0) + (prevGame?.EloDelta ?? 0),
                    CardTime = carryCardTimes ? Math.Max(prevGame?.CardTime ?? 0, 0) : 0,
                    CardTimeRemaining = carryCardTimes ? Math.Max(prevGame?.CardTimeRemaining ?? 0, 0) : 0
                });
            }
        }

        db.SaveChanges();
        return game;
    }

    public static void Resolve(int gameNumber) {
        var db = new HandballContext();
        var game = db.Games.Where(g => g.GameNumber == gameNumber).IncludeRelevant().Include(g => g.Events).First();
        if (!game.Ended) throw new InvalidOperationException("The game has not ended");
        if (Game.ResolvedStatuses.Contains(game.AdminStatus))
            throw new InvalidOperationException("The game is resolved");
        var gameEvent = SetUpGameEvent(game, GameEventType.Resolve, null, null);
        game.AdminStatus = "Resolved";
        game.Resolved = true;
        db.Add(gameEvent);
        db.SaveChanges();
    }
}