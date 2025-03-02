namespace HandballBackend.Models.SendableTypes;

public record GameEventData {
    public int id;
    public string eventType;
    public bool firstTeam;
    public PersonData player;
    public int? details;
    public string? notes;
    public bool firstTeamJustServed;
    public string sideServed;
    public bool firstTeamToServe;
    public string sideToServe;
    public PersonData? teamOneLeft;
    public PersonData? teamOneRight;
    public PersonData? teamTwoLeft;
    public PersonData? teamTwoRight;
    public GamesData? game;

    public GameEventData(GameEvent gameEvent, bool includeGame = false) {
        id = gameEvent.Id;
        eventType = gameEvent.EventType;
        var teamOneId = gameEvent.Game.TeamOneId;
        firstTeam = gameEvent.TeamId == teamOneId;
        player = gameEvent.Player.toSendableData();
        details = gameEvent.Details;
        notes = gameEvent.Notes;
        firstTeamJustServed = gameEvent.TeamWhoServedId == teamOneId;
        sideServed = gameEvent.SideServed;
        firstTeamToServe = gameEvent.TeamToServeId == teamOneId;
        sideToServe = gameEvent.SideToServe;
        teamOneLeft = gameEvent.TeamOneLeft.toSendableData();
        teamOneRight = gameEvent.TeamOneRight.toSendableData();
        teamTwoLeft = gameEvent.TeamTwoLeft.toSendableData();
        teamTwoRight = gameEvent.TeamTwoRight.toSendableData();
        if (includeGame) {
            game = gameEvent.Game.ToSendableData();
        }
    }
}