// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;

namespace HandballBackend.Database.SendableTypes;

public class GameTeamData : TeamData {
    public bool servingFromLeft { get; private set; } = true;

    public new GamePlayerData? captain { get; set; }
    public new GamePlayerData? nonCaptain { get; set; }
    public new GamePlayerData? substitute { get; set; }

    public GameTeamData(
        Team team,
        Game game,
        bool generateStats = false,
        bool formatData = false) : base(team) {
        var startGame = game.Events.FirstOrDefault(a => a.EventType == "Start");
        var lastTimeServed = game.Events
            .OrderByDescending(a => a.Id)
            .FirstOrDefault(a => a.EventType == "Score" && a.TeamToServeId == team.Id);
        if (startGame is null) {
            servingFromLeft = true;
        } else if (game.Tournament.BadmintonServes) {
            if (lastTimeServed is not null) {
                servingFromLeft = lastTimeServed.SideToServe == "Left";
            } else {
                servingFromLeft = true;
            }
        } else {
            if (lastTimeServed is not null) {
                var lastScore = game.Events
                    .OrderByDescending(a => a.Id)
                    .FirstOrDefault(a => a.EventType == "Score");
            } else {
                servingFromLeft = startGame.TeamToServeId == team.Id;
            }
        }

        captain = game.Players.FirstOrDefault(pgs => pgs.PlayerId == team.CaptainId)
            ?.ToSendableData(generateStats, formatData);
        nonCaptain = game.Players.FirstOrDefault(pgs => pgs.PlayerId == team.NonCaptainId)
            ?.ToSendableData(generateStats, formatData);
        substitute = game.Players.FirstOrDefault(pgs => pgs.PlayerId == team.SubstituteId)
            ?.ToSendableData(generateStats, formatData);


        if (!generateStats) return;

        stats = new Dictionary<string, dynamic> {
            ["Timeouts Called"] = game.TeamOneId == team.Id ? game.TeamOneTimeouts : game.TeamTwoTimeouts,
            ["Points Against"] = game.TeamOneId == team.Id ? game.TeamTwoScore : game.TeamOneScore,
            ["Green Cards"] = 0.0,
            ["Yellow Cards"] = 0.0,
            ["Red Cards"] = 0.0,
            ["Faults"] = 0.0,
            ["Double Faults"] = 0.0,
            ["Points Scored"] = 0.0
        };


        foreach (var pgs in game.Players.Where(pgs => pgs.TeamId == team.Id)) {
            stats["Green Cards"] += pgs.GreenCards;
            stats["Yellow Cards"] += pgs.YellowCards;
            stats["Red Cards"] += pgs.RedCards;
            stats["Faults"] += pgs.Faults;
            stats["Double Faults"] += pgs.DoubleFaults;
            stats["Points Scored"] += pgs.PointsScored;
        }

        if (!formatData) return;
        FormatData();
    }
}