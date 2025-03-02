// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 

namespace HandballBackend.Models.SendableTypes;

public record PlayerGameStatsData {
    public readonly TeamData team;
    public readonly bool isBestPlayer;
    public readonly int cardTime;
    public readonly int cardTimeRemaining;
    public readonly string? sideOfCourt;
    public readonly bool isCaptain;
    public readonly string? startSide;
    public readonly Dictionary<string, int> stats = new();

    public PlayerGameStatsData(PlayerGameStats pgs) {
        team = pgs.Team.ToSendableData();
        isBestPlayer = pgs.Game.BestPlayerId == pgs.PlayerId;
        cardTime = pgs.CardTime;
        cardTimeRemaining = pgs.CardTimeRemaining;
        sideOfCourt = pgs.SideOfCourt;
        isCaptain = pgs.Team.CaptainId == pgs.PlayerId;
        startSide = pgs.StartSide;
        stats["Rounds on Court"] = pgs.RoundsOnCourt;
        stats["Rounds Carded"] = pgs.RoundsCarded;
        stats["Points Scored"] = pgs.PointsScored;
        stats["Aces Scored"] = pgs.AcesScored;
        stats["Faults"] = pgs.Faults;
        stats["Double Faults"] = pgs.DoubleFaults;
        stats["Served Points"] = pgs.ServedPoints;
        stats["Served Points Won"] = pgs.ServedPointsWon;
        stats["Serves Received"] = pgs.ServesReceived;
        stats["Serves Returned"] = pgs.ServesReturned;
        stats["Biggest Ace Streak"] = pgs.AceStreak;
        stats["Biggest Serve Streak"] = pgs.ServeStreak;
        stats["Green Cards"] = pgs.GreenCards;
        stats["Yellow Cards"] = pgs.YellowCards;
        stats["Red Cards"] = pgs.RedCards;
    }
};