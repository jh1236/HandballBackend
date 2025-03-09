// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using HandballBackend.Database.Models;
using HandballBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.SendableTypes;

public class PersonData {
    public string name { get; protected set; }
    public string searchableName { get; protected set; }
    public string imageUrl { get; protected set; }
    public string bigImageUrl { get; protected set; }

    private static readonly string[] PercentageColumns = [
        "Percentage of Points Scored",
        "Percentage",
        "Percentage of Points Scored For Team",
        "Percentage of Points Served Won",
        "Serve Return Rate"
    ];

    public Dictionary<string, dynamic>? stats { get; protected set; }


    public PersonData(Person person, Tournament? tournament = null, bool generateStats = false, Team? team = null,
        bool format = false) {
        name = person.Name;
        searchableName = person.SearchableName;
        imageUrl = person.ImageUrl;
        bigImageUrl = person.ImageUrl; //TODO: fix this later

        if (!generateStats) return;

        stats = new Dictionary<string, dynamic> {
            {"B&F Votes", 0.0},
            {"Elo", 0.0},
            {"Games Won", 0.0},
            {"Games Lost", 0.0},
            {"Games Played", 0.0},
            {"Percentage", 0.0},
            {"Points Scored", 0.0},
            {"Points Served", 0.0},
            {"Aces Scored", 0.0},
            {"Faults", 0.0},
            {"Double Faults", 0.0},
            {"Green Cards", 0.0},
            {"Yellow Cards", 0.0},
            {"Red Cards", 0.0},
            {"Rounds on Court", 0.0},
            {"Rounds Carded", 0.0},
            {"Points per Game", 0.0},
            {"Points per Loss", 0.0},
            {"Aces per Game", 0.0},
            {"Faults per Game", 0.0},
            {"Cards", 0.0},
            {"Cards per Game", 0.0},
            {"Points per Card", 0.0},
            {"Serves per Game", 0.0},
            {"Serves per Ace", 0.0},
            {"Serves per Fault", 0.0},
            {"Serve Ace Rate", 0.0},
            {"Serve Fault Rate", 0.0},
            {"Percentage of Points Scored", 0.0},
            {"Percentage of Points Scored For Team", 0.0},
            {"Percentage of Points Served Won", 0.0},
            {"Serves Received", 0.0},
            {"Serves Returned", 0.0},
            {"Max Serve Streak", 0.0},
            {"Max Ace Streak", 0.0},
            {"Serve Return Rate", 0.0},
            {"Votes per 100 games", 0.0},
        };
        var teamPoints = 0;
        var servedPointsWon = 0;
        foreach (var pgs in person.PlayerGameStats ?? []) {
            if (tournament != null && pgs.TournamentId != tournament.Id) continue;
            if (team != null && pgs.TeamId != team.Id) continue;
            if (!pgs.Game.Ranked) continue;
            var game = pgs.Game;
            servedPointsWon += pgs.ServedPointsWon;
            teamPoints += game.TeamOneId == pgs.TeamId ? game.TeamOneScore : game.TeamTwoScore;
            stats["B&F Votes"] += pgs.IsBestPlayer ? 1 : 0;
            stats["Games Won"] += game.Ended && game.WinningTeamId == pgs.TeamId ? 1 : 0;
            stats["Games Lost"] += game.Ended && game.WinningTeamId != pgs.TeamId ? 1 : 0;
            stats["Games Played"] += game.Ended ? 1 : 0;
            stats["Points Scored"] += pgs.PointsScored;
            stats["Points Served"] += pgs.ServedPoints;
            stats["Aces Scored"] += pgs.AcesScored;
            stats["Faults"] += pgs.Faults;
            stats["Double Faults"] += pgs.DoubleFaults;
            stats["Green Cards"] += pgs.GreenCards;
            stats["Yellow Cards"] += pgs.YellowCards;
            stats["Red Cards"] += pgs.RedCards;
            stats["Rounds on Court"] += pgs.RoundsOnCourt;
            stats["Rounds Carded"] += pgs.RoundsCarded;
            stats["Cards"] += pgs.GreenCards + pgs.YellowCards + pgs.RedCards;
            stats["Serves Received"] += pgs.ServesReceived;
            stats["Serves Returned"] += pgs.ServesReturned;
            stats["Max Ace Streak"] = Math.Max(stats["Max Ace Streak"], pgs.AceStreak);
            stats["Max Serve Streak"] = Math.Max(stats["Max Serve Streak"], pgs.ServeStreak);
        }

        var gamesPlayed = Math.Max(stats["Games Played"], 1);
        stats["Percentage"] = stats["Games Won"] / Math.Max(stats["Games Played"], 1.0);
        stats["Points per Game"] = stats["Points Scored"] / gamesPlayed;
        stats["Points per Loss"] = stats["Points Scored"] / Math.Max(stats["Games Lost"], 1);
        stats["Aces per Game"] = stats["Aces Scored"] / gamesPlayed;
        stats["Faults per Game"] = stats["Faults"] / gamesPlayed;
        stats["Cards per Game"] = stats["Cards"] / gamesPlayed;
        stats["Points per Card"] = stats["Points Scored"] / Math.Max(stats["Cards"], 1);
        stats["Serves per Game"] = stats["Points Served"] / gamesPlayed;
        stats["Serves per Ace"] = stats["Points Served"] / Math.Max(stats["Aces Scored"], 1);
        stats["Serves per Fault"] = stats["Points Served"] / Math.Max(stats["Faults"], 1);
        stats["Serves Ace Rate"] = stats["Aces Scored"] / Math.Max(stats["Points Served"], 1);
        stats["Serves Fault Rate"] = stats["Faults"] / Math.Max(stats["Points Served"], 1);
        stats["Percentage of Points Scored"] =
            stats["Points Scored"] / Math.Max(stats["Rounds on Court"], 1);
        stats["Percentage of Points Scored For Team"] = stats["Points Scored"] / Math.Max(teamPoints, 1);
        stats["Percentage of Served Points Won"] =
            servedPointsWon / Math.Max(stats["Points Served"], 1);
        stats["Serve Return Rate"] = stats["Serves Returned"] / Math.Max(stats["Serves Received"], 1);
        stats["Votes per 100 games"] = 100.0f * stats["B&F Votes"] / gamesPlayed;
        
        if (!format) return;
        
        FormatData();
    }

    protected void FormatData() {
        foreach (var stat in stats!.Keys) {
            if (stats[stat] == null) continue;
            if (PercentageColumns.Contains(stat)) {
                stats[stat] = Math.Round(100.0 * (double) stats[stat], 2) + "%";
            }
            else {
                stats[stat] = Math.Round((double) stats[stat], 2).ToString();
            }
        }
    }
}