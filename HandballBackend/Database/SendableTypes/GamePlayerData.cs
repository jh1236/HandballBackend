﻿// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;
using HandballBackend.Models;

namespace HandballBackend.Database.SendableTypes;

public class GamePlayerData : PersonData {
    public bool isBestPlayer { get; set; }
    public int cardTime { get; set; }
    public int cardTimeRemaining { get; set; }
    public string? sideOfCourt { get; set; }
    public bool isCaptain { get; set; }
    public string startSide { get; set; }

    public GamePlayerData(Person player, Game game, bool includeStats = false, bool formatData = false)
        : this(game.Players.First(p => p.PlayerId == player.Id), includeStats, formatData) {
    }

    public GamePlayerData(PlayerGameStats pgs, bool includeStats = false, bool formatData = false)
        : base(pgs.Player) {
        isBestPlayer = pgs.IsBestPlayer;
        cardTime = pgs.CardTime;
        cardTimeRemaining = pgs.CardTimeRemaining;
        sideOfCourt = pgs.SideOfCourt;
        isCaptain = pgs.Id == pgs.Team.CaptainId;
        startSide = pgs.StartSide;
        if (!includeStats) return;
        stats = new Dictionary<string, dynamic> {
            ["B&F Votes"] = pgs.IsBestPlayer,
            ["Games Won"] = pgs.Game.Ended && pgs.Game.WinningTeamId == pgs.TeamId ? 1 : 0,
            ["Games Lost"] = pgs.Game.Ended && pgs.Game.WinningTeamId != pgs.TeamId ? 1 : 0,
            ["Games Played"] = pgs.Game.Ended ? 1 : 0,
            ["Points Scored"] = pgs.PointsScored,
            ["Points Served"] = pgs.ServedPoints,
            ["Aces Scored"] = pgs.AcesScored,
            ["Faults"] = pgs.Faults,
            ["Double Faults"] = pgs.DoubleFaults,
            ["Green Cards"] = pgs.GreenCards,
            ["Yellow Cards"] = pgs.YellowCards,
            ["Red Cards"] = pgs.RedCards,
            ["Rounds on Court"] = pgs.RoundsOnCourt,
            ["Rounds Carded"] = pgs.RoundsCarded,
            ["Cards"] = pgs.GreenCards + pgs.YellowCards + pgs.RedCards,
            ["Serves Received"] = pgs.ServesReceived,
            ["Serves Returned"] = pgs.ServesReturned,
            ["Max Ace Streak"] = pgs.AceStreak,
            ["Max Serve Streak"] = pgs.ServeStreak
        };
        if (!formatData) return;
        FormatData();
    }
}