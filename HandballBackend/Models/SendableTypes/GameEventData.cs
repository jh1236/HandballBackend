// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 


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
    public GameData? game;

    public GameEventData(GameEvent gameEvent, bool includeGame = false) {
        var teamOneId = gameEvent.Game.TeamOneId;

        id = gameEvent.Id;
        eventType = gameEvent.EventType;
        firstTeam = gameEvent.TeamId == teamOneId;
        player = gameEvent.Player.ToSendableData();
        details = gameEvent.Details;
        notes = gameEvent.Notes;
        firstTeamJustServed = gameEvent.TeamWhoServedId == teamOneId;
        sideServed = gameEvent.SideServed;
        firstTeamToServe = gameEvent.TeamToServeId == teamOneId;
        sideToServe = gameEvent.SideToServe;
        teamOneLeft = gameEvent.TeamOneLeft.ToSendableData();
        teamOneRight = gameEvent.TeamOneRight.ToSendableData();
        teamTwoLeft = gameEvent.TeamTwoLeft.ToSendableData();
        teamTwoRight = gameEvent.TeamTwoRight.ToSendableData();
        if (includeGame) {
            game = gameEvent.Game.ToSendableData();
        }
    }
}