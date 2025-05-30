﻿using HandballBackend.Database.Models;
using HandballBackend.Utils;

namespace HandballBackend.Database.SendableTypes;

public class GameEventData {
    public int Id { get; private set; }
    public string EventType { get; private set; }
    public bool? FirstTeam { get; private set; }
    public PersonData? Player { get; private set; }
    public int? Details { get; private set; }
    public string? Notes { get; private set; }
    public bool? FirstTeamJustServed { get; private set; }
    public string? SideServed { get; private set; }
    public bool FirstTeamToServe { get; private set; }
    public string SideToServe { get; private set; }

    public GameData? Game { get; private set; }

    public GameEventData(GameEvent gameEvent, bool includeGame = false) {
        var teamOneId = gameEvent.Game.TeamOneId;

        Id = gameEvent.Id;
        EventType = Utilities.SplitCamelCase(gameEvent.EventType.ToString());
        FirstTeam = gameEvent.TeamId == teamOneId;
        Player = gameEvent.Player?.ToSendableData();
        Details = gameEvent.Details;
        Notes = gameEvent.Notes;
        FirstTeamJustServed = gameEvent.TeamWhoServedId == teamOneId;
        SideServed = gameEvent.SideServed;
        FirstTeamToServe = gameEvent.TeamToServeId == teamOneId;
        SideToServe = gameEvent.SideToServe;
        if (includeGame) {
            Game = gameEvent.Game.ToSendableData();
        }
    }
}