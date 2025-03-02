// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 


namespace HandballBackend.Models.SendableTypes;

public record GameData {
    public readonly int id;
    public readonly TournamentData tournament;
    public readonly TeamData teamOne;
    public readonly TeamData teamTwo;
    public readonly int teamOneScore;
    public readonly int teamTwoScore;
    public readonly int teamOneTimeouts;
    public readonly int teamTwoTimeouts;
    public readonly bool firstTeamWinning;
    public readonly bool started;
    public readonly bool someoneHasWon;
    public readonly bool ended;
    public readonly bool protested;
    public readonly bool resolved;
    public readonly bool ranked;
    public readonly PersonData bestPlayer;
    public readonly OfficialData? official;
    public readonly OfficialData? scorer;
    public readonly bool firstTeamIga;
    public readonly bool firstTeamToServe;
    public readonly string sideToServe;
    public readonly int? startTime;
    public readonly int? serveTimer;
    public readonly int? length;
    public readonly bool isFinal;
    public readonly int round;
    public readonly bool isBye;
    public readonly string status;
    public readonly bool faulted;
    public readonly int changecode;
    public readonly int? timeoutExpirationTime;
    public readonly bool isOfficialTimeout;
    public readonly GameData? game;


    public GameData(Game game, bool isAdmin = false, bool includeGame = false) {
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
        if (includeGame) {
            this.game = game.ToSendableData();
        }
    }
}