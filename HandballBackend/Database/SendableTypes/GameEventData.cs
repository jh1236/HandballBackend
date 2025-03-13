// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;
using HandballBackend.Models;
using HandballBackend.Utils;

namespace HandballBackend.Database.SendableTypes;

public class GameEventData {
    public int id { get; private set; }
    public string eventType { get; private set; }
    public bool? firstTeam { get; private set; }
    public PersonData? player { get; private set; }
    public int? details { get; private set; }
    public string? notes { get; private set; }
    public bool? firstTeamJustServed { get; private set; }
    public string? sideServed { get; private set; }
    public bool firstTeamToServe { get; private set; }
    public string sideToServe { get; private set; }

    public GameData? game { get; private set; }

    public GameEventData(GameEvent gameEvent, bool includeGame = false) {
        var teamOneId = gameEvent.Game.TeamOneId;

        id = gameEvent.Id;
        eventType = Utilities.SplitCamelCase(gameEvent.EventType.ToString());
        firstTeam = gameEvent.TeamId == teamOneId;
        player = gameEvent.Player?.ToSendableData();
        details = gameEvent.Details;
        notes = gameEvent.Notes;
        firstTeamJustServed = gameEvent.TeamWhoServedId == teamOneId;
        sideServed = gameEvent.SideServed;
        firstTeamToServe = gameEvent.TeamToServeId == teamOneId;
        sideToServe = gameEvent.SideToServe;
        if (includeGame) {
            game = gameEvent.Game.ToSendableData();
        }
    }
}