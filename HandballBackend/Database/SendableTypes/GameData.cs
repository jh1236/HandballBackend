// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;

namespace HandballBackend.Database.SendableTypes;

public class AdminGameData {
    private static readonly string[] NO_ACTION_REQUIRED = {
        "Resolved", "In Progress", "Official", "Ended", "Waiting For Start", "Forfeit", "Bye"
    };

    public bool markedForReview { get; set; }
    public bool requiresAction { get; set; }
    public string noteableStatus { get; set; }
    public string notes { get; set; }
    public int teamOneRating { get; set; }
    public int teamTwoRating { get; set; }
    public string? teamOneNotes { get; set; }
    public string? teamTwoNotes { get; set; }
    public string? teamOneProtest { get; set; }
    public string? teamTwoProtest { get; set; }
    public GameEventData[] cards { get; set; }
    public bool resolved { get; set; }

    public AdminGameData(Game game) {
        var teamNotes = game.Events.Where(a => a.EventType is GameEventType.Notes).ToArray();
        var protests = game.Events.Where(a => a.EventType is GameEventType.Protest).ToArray();
        var cardEvemts = game.Events.Where(a => a.IsCard);
        markedForReview = game.MarkedForReview;
        requiresAction = !NO_ACTION_REQUIRED.Contains(game.AdminStatus);
        noteableStatus = game.NoteableStatus;
        notes = (game.Notes?.Trim().Length ?? 0) > 0 ? game.Notes!.Trim() : "";
        teamOneRating = teamNotes
            .Where(ge => ge.TeamId == game.TeamOneId)
            .Select(gE => gE.Details ?? 3)
            .FirstOrDefault(3);
        teamTwoRating = teamNotes
            .Where(ge => ge.TeamId == game.TeamTwoId)
            .Select(gE => gE.Details ?? 3)
            .FirstOrDefault(3);
        teamOneNotes = teamNotes
            .Where(ge => ge.TeamId == game.TeamOneId && ge.Notes != null)
            .Select(gE => gE.Notes)
            .FirstOrDefault();
        teamTwoNotes = teamNotes
            .Where(ge => ge.TeamId == game.TeamTwoId && ge.Notes != null)
            .Select(gE => gE.Notes)
            .FirstOrDefault();
        teamOneProtest = protests
            .Where(ge => ge.TeamId == game.TeamOneId && ge.Notes != null)
            .Select(gE => gE.Notes)
            .FirstOrDefault();
        teamTwoProtest = protests
            .Where(ge => ge.TeamId == game.TeamTwoId && ge.Notes != null)
            .Select(gE => gE.Notes)
            .FirstOrDefault();
        cards = cardEvemts.Select(a => a.ToSendableData()).ToArray();
    }
}

public class GameData {
    public int id { get; private set; }
    public TournamentData? tournament { get; private set; }
    public GameTeamData teamOne { get; private set; }
    public GameTeamData teamTwo { get; private set; }
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
    public int changeCode { get; private set; }
    public long? timeoutExpirationTime { get; private set; }
    public bool isOfficialTimeout { get; private set; }

    public GameEventData[]? events { get; private set; } = null;

    public AdminGameData admin { get; private set; }
    public int court { get; private set; }


    public GameData(
        Game game,
        bool includeTournament = false,
        bool includeGameEvents = false,
        bool includeStats = false,
        bool formatData = false,
        bool isAdmin = false
    ) {
        id = game.GameNumber;
        tournament = includeTournament ? game.Tournament?.ToSendableData() : null;
        teamOne = game.TeamOne?.ToGameSendableData(game, includeStats, formatData, isAdmin);
        teamTwo = game.TeamTwo?.ToGameSendableData(game, includeStats, formatData, isAdmin);
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
            .Where(a => a.EventType is GameEventType.Fault or GameEventType.Score)
            .OrderBy(a => a.Id)
            .Select(a => a.EventType == GameEventType.Fault)
            .LastOrDefault(false);
        changeCode = game.Events.Select(a => a.Id).OrderByDescending(a => a).FirstOrDefault(game.Id);
        timeoutExpirationTime = game.Events
            .Where(a => a.EventType is GameEventType.Timeout or GameEventType.EndTimeout)
            .OrderBy(a => a.Id)
            .Select(a =>
                    a.EventType == GameEventType.Timeout
                        ? a.TeamId is null // the event is a timeout
                            ? a.CreatedAt // the event is an official timeout
                            : (a.CreatedAt + Config.TimeoutTime) * 1000 // the event is a normal timeout
                        : -1 // the event is an `end timeout`
            )
            .LastOrDefault(-1);
        isOfficialTimeout = game.Events
            .Where(a => a.EventType is GameEventType.Timeout)
            .Select(a => a.TeamId is null)
            .LastOrDefault(false);
        court = game.Court;


        if (includeGameEvents) {
            events = game.Events.Select(a => a.ToSendableData(false)).ToArray();
        }

        if (isAdmin) {
            admin = new AdminGameData(game);
        }
    }
}