// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 


namespace HandballBackend.Models.SendableTypes;

public record GameData {
    public int id;
    public TournamentData tournament;
    public TeamData teamOne;
    public TeamData teamTwo;
    public int teamOneScore;
    public int teamTwoScore;
    public int teamOneTimeouts;
    public int teamTwoTimeouts;
    public bool firstTeamWinning;
    public bool started;
    public bool someoneHasWon;
    public bool ended;
    public bool protested;
    public bool resolved;
    public bool ranked;
    public PersonData bestPlayer;
    public OfficialData? official;
    public OfficialData? scorer;
    public bool firstTeamIga;
    public bool firstTeamToServe;
    public string sideToServe;
    public int? startTime;
    public int? serveTimer;
    public int? length;
    public bool isFinal;
    public int round;
    public bool isBye;
    public string status;
    public bool faulted;
    public int changecode;
    public int? timeoutExpirationTime;
    public bool isOfficialTimeout;


    public GameData(Game game, bool isAdmin = false) {
        id = game.Id;
        tournament = game.Tournament.ToSendableData();
        teamOne = game.TeamOne.ToSendableData();
        teamTwo = game.TeamTwo.ToSendableData();
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
        bestPlayer = game.BestPlayer.ToSendableData();
        official = game.Official.ToSendableData();
        scorer = game.Scorer.ToSendableData();
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
            .Select(a => a.EventType)
            .LastOrDefault("Score") == "Fault";
        changecode = game.Events.OrderBy(a => a.Id).Last().Id;
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