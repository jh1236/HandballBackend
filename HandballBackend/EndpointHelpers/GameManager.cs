using HandballBackend.Database;
using HandballBackend.Database.Models;
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


    private static void SyncGame(HandballContext db, Game game) {
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

        var faulted = false;
        var aceStreak = 0;
        var serveStreak = 1;
        foreach (var gameEvent in game.Events) {
            var isFirstTeam = gameEvent.TeamId == game.TeamOneId;
            var playersOnCourt = game.Players.Where(p =>
                p.PlayerId == gameEvent.TeamOneLeftId ||
                p.PlayerId == gameEvent.TeamOneRightId ||
                p.PlayerId == gameEvent.TeamTwoLeftId ||
                p.PlayerId == gameEvent.TeamTwoRightId).ToArray();
            var player = game.Players.FirstOrDefault(p => p.PlayerId == gameEvent.PlayerId);
            var leftServed = gameEvent.SideServed == "Left";
            var nonServingTeam = playersOnCourt.Where(pgs => gameEvent.TeamWhoServedId != pgs.TeamId).OrderBy(pgs =>
                    pgs.PlayerId != gameEvent.TeamOneLeftId && pgs.PlayerId != gameEvent.TeamTwoLeftId)
                .Select(PlayerGameStats? (pgs) => pgs)
                .ToList(); //force the team into LTR order
            nonServingTeam.Add(null);
            switch (gameEvent.EventType) {
                case GameEventType.Start:
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

                    break;
                case GameEventType.EndGame:
                    game.BestPlayerId = gameEvent.Details;
                    foreach (var pgs in game.Players) {
                        pgs.IsBestPlayer = pgs.PlayerId == gameEvent.Details;
                    }

                    game.Notes = gameEvent.Notes;
                    game.Ended = true;
                    break;
                case GameEventType.Score:
                    faulted = false;
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

                    break;
                case GameEventType.Fault:
                    player.Faults += 1;
                    player.ServedPoints += 1;
                    if (faulted) {
                        player.DoubleFaults += 1;
                        faulted = false;
                    } else {
                        faulted = true;
                    }

                    break;
                case GameEventType.Timeout:
                    if (isFirstTeam) {
                        game.TeamOneTimeouts += 1;
                    } else {
                        game.TeamTwoTimeouts += 1;
                    }

                    break;

                case GameEventType.Forfeit:
                    if (isFirstTeam) {
                        game.TeamTwoScore = Math.Min(Math.Max(game.TeamOneScore + 2, 11), 18);
                    } else {
                        game.TeamOneScore = Math.Min(Math.Max(game.TeamTwoScore + 2, 11), 18);
                    }

                    break;
                case GameEventType.Warning:
                    player.Warnings += 1;
                    break;
                case GameEventType.GreenCard:
                    player.GreenCards += 1;
                    if (player.CardTimeRemaining >= 0) {
                        //player isn't already red carded
                        player.CardTimeRemaining += gameEvent.Details ?? 0;
                        player.CardTime = player.CardTimeRemaining;
                    }

                    break;
                case GameEventType.YellowCard:
                    player.YellowCards += 1;
                    if (player.CardTimeRemaining >= 0) {
                        //player isn't already red carded
                        player.CardTimeRemaining += gameEvent.Details ?? 0;
                        player.CardTime = player.CardTimeRemaining;
                    }

                    break;
                case GameEventType.RedCard:
                    player.RedCards += 1;
                    player.CardTimeRemaining += -1;
                    player.CardTime = -1;
                    break;
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

        var lastEvent = game.Events.OrderByDescending(gE => gE.Id).First();
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

        var highScore = Math.Max(game.TeamOneScore, game.TeamTwoScore);
        if (highScore > 11 && (Math.Abs(game.TeamOneScore - game.TeamTwoScore) >= 2 || highScore >= 18)) {
            game.SomeoneHasWon = true;
        }
    }

    private static void AddPointToGame(HandballContext db, Game game, bool firstTeam, string? playerSearchable,
        bool penalty = false, string? notes = null) {
        var teamId = firstTeam ? game.TeamOneId : game.TeamTwoId;
        var lastService = game.Events.Where(gE => gE.TeamToServeId == teamId).OrderByDescending(gE => gE.Id)
            .FirstOrDefault();
        var prevEvent = game.Events.OrderByDescending(gE => gE.Id).First();
        var player = game.Players.FirstOrDefault(pgs => pgs.Player.SearchableName == playerSearchable);
        var newEvent = new GameEvent {
            TeamId = teamId,
            PlayerId = playerSearchable != null ? player!.PlayerId : null,
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
            // this is our first win of the service, so we need to make changes accordingly
            // This is identical between games with and without baddy serves, so no check is required
            var defaultSide = game.Tournament.BadmintonServes ? "Left" : "Right";
            //I wrote the above comment then remembered that you start on a different side as second team
            newEvent.SideToServe = (lastService?.SideToServe ?? defaultSide) == "Left" ? "Right" : "Left";
            newEvent.PlayerToServeId = game.Players
                .First(pgs => pgs.TeamId == teamId && pgs.SideOfCourt == newEvent.SideToServe).PlayerId;
        }
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
        SyncGame(db, game);
        db.SaveChanges();
    }

    public static void ScorePoint(int gameNumber, bool firstTeam, bool leftPlayer, string? scoreMethod) {
        
    }

    public static void ScorePoint(int gameNumber, bool firstTeam, string playerSearchable, string? scoreMethod) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().First(g => g.GameNumber == gameNumber);
        if (game.Ended) throw new InvalidOperationException("The game has ended");
        if (!VALID_SCORE_METHODS.Contains(scoreMethod)) {
            throw new ArgumentException("The score method provided is invalid");
        }
        AddPointToGame(db, game, firstTeam, playerSearchable, notes: scoreMethod);
    }
}