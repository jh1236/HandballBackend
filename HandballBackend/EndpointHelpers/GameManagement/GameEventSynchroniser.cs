using HandballBackend.Database;
using HandballBackend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.EndpointHelpers.GameManagement;

internal static class GameEventSynchroniser {
    public static void SyncGame(HandballContext db, int gameNumber) {
        var game = db.Games.IncludeRelevant().Include(g => g.Events).SingleOrDefault(g => g.GameNumber == gameNumber);
        if (game is null) throw new InvalidOperationException($"Game {gameNumber} not found");
        game.Reset();
        var carryOverCards = game.TournamentId >= 8;
        foreach (var pgs in game.Players) {
            pgs.ResetStats();
            if (!carryOverCards) continue;
            var prevGamePlayer = db.PlayerGameStats
                .FirstOrDefault(prev => prev.TournamentId == game.TournamentId && pgs.PlayerId == prev.PlayerId);
            var cardTime = prevGamePlayer?.RedCards == 0 ? Math.Max(prevGamePlayer?.CardTimeRemaining ?? 0, 0) : -1;
            pgs.CardTimeRemaining = cardTime;
            pgs.CardTime = cardTime;
        }

        foreach (var gameEvent in game.Events.OrderBy(g => g.Id)) {
            switch (gameEvent.EventType) {
                case GameEventType.Start:
                    SyncStartGame(game, gameEvent);
                    break;
                case GameEventType.EndGame:
                    SyncEndGame(game, gameEvent);
                    break;
                case GameEventType.Score:
                    SyncScorePoint(game, gameEvent);
                    break;
                case GameEventType.Fault:
                    SyncFault(game, gameEvent);
                    break;
                case GameEventType.Timeout:
                    SyncTimeout(game, gameEvent);
                    break;
                case GameEventType.Forfeit:
                    SyncForfeit(game, gameEvent);
                    break;
                case GameEventType.Warning:
                    SyncCard(game, gameEvent);
                    break;
                case GameEventType.GreenCard:
                    SyncCard(game, gameEvent);
                    break;
                case GameEventType.YellowCard:
                    SyncCard(game, gameEvent);
                    break;
                case GameEventType.RedCard:
                    SyncCard(game, gameEvent);
                    break;
                case GameEventType.Protest:
                    game.Protested = true;
                    break;
                case GameEventType.Resolve:
                    game.Resolved = true;
                    break;
                case GameEventType.Notes:
                case GameEventType.Substitute:
                case GameEventType.EndTimeout:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var lastEvent = game.Events.OrderByDescending(gE => gE.Id).FirstOrDefault();
        foreach (var pgs in game.Players) {
            if (lastEvent == null) {
                pgs.SideOfCourt = pgs.StartSide;
            } else if (pgs.PlayerId == lastEvent.TeamTwoLeftId || pgs.PlayerId == lastEvent.TeamOneLeftId) {
                pgs.SideOfCourt = "Left";
            } else if (pgs.PlayerId == lastEvent.TeamTwoRightId || pgs.PlayerId == lastEvent.TeamOneRightId) {
                pgs.SideOfCourt = "Right";
            } else {
                pgs.SideOfCourt = "Substitute";
            }
        }

        if (lastEvent != null) {
            game.TeamToServeId = lastEvent.TeamToServeId;
            game.SideToServe = lastEvent.SideToServe;
            game.PlayerToServeId = lastEvent.PlayerToServeId;
        }
    }

    public static void SyncCard(Game game, GameEvent gameEvent) {
        var player = game.Players.FirstOrDefault(p => p.PlayerId == gameEvent.PlayerId)!;
        switch (gameEvent.EventType) {
            case GameEventType.Warning:
                player.Warnings += 1;
                break;
            case GameEventType.GreenCard:
                player.GreenCards += 1;
                break;
            case GameEventType.YellowCard:
                player.YellowCards += 1;
                break;
            case GameEventType.RedCard:
                player.RedCards += 1;
                break;
            default:
                throw new ArgumentException("Only card events can be synced as cards");
        }

        player.GreenCards += 1;
        if (player.CardTimeRemaining < 0) return;
        //player isn't already red carded

        if (gameEvent.Details > 0) {
            //the card is not a red or warning
            player.CardTimeRemaining += gameEvent.Details ?? 0;
            player.CardTime = player.CardTimeRemaining;
        } else if (gameEvent.Details < 0) {
            //red card
            player.CardTimeRemaining = -1;
        }
    }


    public static void SyncFault(Game game, GameEvent gameEvent) {
        var player = game.Players.FirstOrDefault(pgs => pgs.PlayerId == gameEvent.PlayerId);
        var faulted = game.Events.OrderByDescending(gE => gE.EventType is GameEventType.Fault or GameEventType.Score)
            .Select(gE => gE.EventType is GameEventType.Fault).FirstOrDefault(false);
        player.Faults += 1;
        player.ServedPoints += 1;
        if (faulted) {
            player.DoubleFaults += 1;
        }
    }

    public static void SyncScorePoint(Game game, GameEvent gameEvent) {
        var player = game.Players.FirstOrDefault(pgs => pgs.PlayerId == gameEvent.PlayerId);
        var playersOnCourt = game.Players.Where(p =>
            p.PlayerId == gameEvent.TeamOneLeftId ||
            p.PlayerId == gameEvent.TeamOneRightId ||
            p.PlayerId == gameEvent.TeamTwoLeftId ||
            p.PlayerId == gameEvent.TeamTwoRightId).ToArray();
        var serveStreak = game.Events.OrderByDescending(ge => ge.Id)
            .TakeWhile(ge => ge.PlayerToServeId == player?.PlayerId).Count();
        var aceStreak = game.Events.OrderByDescending(ge => ge.Id)
            .TakeWhile(gE => gE.PlayerToServeId == player?.PlayerId && gE.Notes == "Ace").Count();

        var nonServingTeam = playersOnCourt.Where(pgs => gameEvent.TeamWhoServedId != pgs.TeamId).OrderBy(pgs =>
                pgs.PlayerId != gameEvent.TeamOneLeftId && pgs.PlayerId != gameEvent.TeamTwoLeftId)
            .Select(PlayerGameStats? (pgs) => pgs)
            .ToList(); //force the team into LTR order
        nonServingTeam.Add(null);
        var leftServed = gameEvent.SideServed == "Left";
        var isFirstTeam = gameEvent.TeamId == game.TeamOneId;
        if (player is null) {
            //EVIL AWFUL EVIL GUARD CLAUSE and dabs
            goto end;
        }

        player.PointsScored += 1;
        var playerWhoServed =
            playersOnCourt.FirstOrDefault(pgs => pgs.PlayerId == gameEvent.PlayerWhoServedId)!;
        playerWhoServed.ServedPoints += 1;
        if (playerWhoServed.TeamId == gameEvent.TeamId) {
            playerWhoServed.ServedPointsWon += 1;
        }

        if (gameEvent.PlayerWhoServedId == gameEvent.PlayerToServeId) {
            serveStreak += 1;
        } else {
            serveStreak = 1;
        }

        if (gameEvent.Notes == "Ace") {
            player.AcesScored += 1;
            if (gameEvent.PlayerWhoServedId == gameEvent.PlayerToServeId) {
                aceStreak += 1;
            } else {
                aceStreak = 1;
            }
        } else {
            aceStreak = 0;
        }

        player.ServeStreak = Math.Max(player.ServeStreak, serveStreak);
        player.AceStreak = Math.Max(player.AceStreak, aceStreak);
        var receivingPlayer = nonServingTeam[leftServed ? 0 : 1] ??
                              nonServingTeam.FirstOrDefault(a => a != null);
        if (receivingPlayer?.CardTimeRemaining != 0) {
            receivingPlayer = nonServingTeam.FirstOrDefault(pgs => (pgs?.CardTimeRemaining ?? 1) == 0);
        }

        if (receivingPlayer != null && gameEvent.Notes != "Penalty") {
            receivingPlayer.ServesReceived += 1;
            if (gameEvent.Notes != "Ace") {
                receivingPlayer.ServesReturned += 1;
            }
        }


        end:
        if (isFirstTeam) {
            game.TeamOneScore += 1;
        } else {
            game.TeamTwoScore += 1;
        }

        game.TeamToServeId = gameEvent.TeamToServeId;
        game.PlayerToServeId = gameEvent.PlayerToServeId;
        game.SideToServe = gameEvent.SideToServe;


        foreach (var pgs in playersOnCourt) {
            if (pgs.CardTimeRemaining == 0) {
                pgs.RoundsOnCourt += 1;
            } else {
                pgs.RoundsCarded += 1;
                if (pgs.CardTime > 0) {
                    pgs.CardTimeRemaining -= 1;
                }
            }
        }

        var highScore = Math.Max(game.TeamOneScore, game.TeamTwoScore);
        if (highScore >= 11 && (Math.Abs(game.TeamOneScore - game.TeamTwoScore) >= 2 || highScore >= 18)) {
            game.SomeoneHasWon = true;
        }
    }

    private static void SyncEndGame(Game game, GameEvent gameEvent) {
        game.BestPlayerId = gameEvent.Details;
        foreach (var pgs in game.Players) {
            pgs.IsBestPlayer = pgs.PlayerId == gameEvent.Details;
        }

        game.Notes = gameEvent.Notes;
        game.Ended = true;
    }


    public static void SyncStartGame(Game game, GameEvent gameEvent) {
        game.Started = true;
        foreach (var pgs in game.Players) {
            if (pgs.PlayerId == gameEvent.TeamOneLeftId || pgs.PlayerId == gameEvent.TeamTwoLeftId) {
                pgs.StartSide = "Left";
            } else if (pgs.PlayerId == gameEvent.TeamOneRightId ||
                       pgs.PlayerId == gameEvent.TeamTwoRightId) {
                pgs.StartSide = "Right";
            } else {
                pgs.StartSide = "Substitute";
            }
        }
    }

    public static void SyncTimeout(Game game, GameEvent gameEvent) {
        if (gameEvent.TeamId == game.TeamOneId) {
            game.TeamOneTimeouts += 1;
        } else {
            game.TeamTwoTimeouts += 1;
        }
    }

    public static void SyncForfeit(Game game, GameEvent gameEvent) {
        if (gameEvent.TeamToServeId == game.TeamOneId) {
            game.TeamTwoScore = Math.Min(Math.Max(game.TeamOneScore + 2, 11), 18);
        } else {
            game.TeamOneScore = Math.Min(Math.Max(game.TeamTwoScore + 2, 11), 18);
        }

        game.SomeoneHasWon = true;
    }
}