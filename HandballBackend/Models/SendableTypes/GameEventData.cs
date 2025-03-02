// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 


namespace HandballBackend.Models.SendableTypes;

public record GameEventData {
    public readonly int id;
    public readonly string eventType;
    public readonly bool firstTeam;
    public readonly PersonData player;
    public readonly int? details;
    public readonly string? notes;
    public readonly bool firstTeamJustServed;
    public readonly string sideServed;
    public readonly bool firstTeamToServe;
    public readonly string sideToServe;
    public readonly PersonData? teamOneLeft;
    public readonly PersonData? teamOneRight;
    public readonly PersonData? teamTwoLeft;
    public readonly PersonData? teamTwoRight;
    public readonly GameData? game;

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