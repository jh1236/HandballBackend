// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;

namespace HandballBackend.Database.SendableTypes;

public class GameData {
    public int id { get; private set; }
    public TournamentData tournament { get; private set; }
    public TeamData teamOne { get; private set; }
    public TeamData teamTwo { get; private set; }
    public int teamOneScore { get; private set; }
    public int teamTwoScore { get; private set; }
    public int teamOneTimeouts { get; private set; }
    public int teamTwoTimeouts { get; private set; }
    public bool firstTeamWinning { get; private set; }
    public bool started { get; private set; }
    public bool someoneHasWon { get; private set; }
    public bool ended { get; private set; }
    public bool protested { get; private set; }
    public bool resolved { get; private set; }
    public bool ranked { get; private set; }
    public PersonData? bestPlayer { get; private set; }
    public OfficialData? official { get; private set; }
    public OfficialData? scorer { get; private set; }
    public bool firstTeamIga { get; private set; }
    public bool firstTeamToServe { get; private set; }
    public string sideToServe { get; private set; }
    public int? startTime { get; private set; }
    public int? serveTimer { get; private set; }
    public int? length { get; private set; }
    public bool isFinal { get; private set; }
    public int round { get; private set; }
    public bool isBye { get; private set; }
    public string status { get; private set; }
    public bool faulted { get; private set; }
    public int changecode { get; private set; }
    public int? timeoutExpirationTime { get; private set; }
    public bool isOfficialTimeout { get; private set; }

    public GameData(Game game, bool isAdmin = false, bool includeGame = false) {
        id = game.Id;
        tournament = game.Tournament?.ToSendableData();
        teamOne = game.TeamOne?.ToSendableData();
        teamTwo = game.TeamTwo?.ToSendableData();
        teamOneScore = game.TeamOneScore;
        teamTwoScore = game.TeamTwoScore;
        teamOneTimeouts = game.TeamOneTimeouts;
        teamTwoTimeouts = game.TeamTwoTimeouts;
        firstTeamWinning = game.WinningTeamId == game.TeamOneId;
        started = game.Started;
        someoneHasWon = game.SomeoneHasWon;
        ended = game.Ended;
        protested = game.Protested;
        ranked = game.Ranked;
        bestPlayer = game.BestPlayer?.ToSendableData();
        official = game.Official?.ToSendableData();
        scorer = game.Scorer?.ToSendableData();
        firstTeamIga = game.TeamOneId == game.IgaSideId;
        firstTeamToServe = game.TeamToServeId == game.TeamOneId;
        sideToServe = game.SideToServe;
        startTime = game.StartTime;
        serveTimer = game.ServeTimer;
        length = game.Length;
        isFinal = game.IsFinal;
        round = game.Round;
        isBye = game.IsBye;
        status = isAdmin ? game.Status : game.AdminStatus;
        faulted = game.Events
            .Where(a => a.EventType is "Fault" or "Score")
            .OrderBy(a => a.Id)
            .Select(a => a.EventType == "Fault")
            .LastOrDefault(false);
        changecode = game.Events.Select(a => a.Id).OrderByDescending(a => a).FirstOrDefault(game.Id);
        timeoutExpirationTime = game.Events
            .Where(a => a.EventType is "Timeout" or "End Timeout")
            .OrderBy(a => a.Id)
            .Select(a =>
                    a.EventType == "Timeout"
                        ? a.TeamId is null // the event is a timeout
                            ? a.CreatedAt // the event is an official timeout
                            : a.CreatedAt + Config.TimeoutTime * 1000 // the event is a normal timeout
                        : -1 // the event is an `end timeout`
            )
            .LastOrDefault(-1);
        isOfficialTimeout = game.Events
            .Where(a => a.EventType is "Timeout")
            .Select(a => a.TeamId is null)
            .LastOrDefault(false);
    }
}