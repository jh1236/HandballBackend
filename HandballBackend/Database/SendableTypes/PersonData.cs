// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using HandballBackend.Database.Models;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.SendableTypes;

public class PersonData {
    public string name { get; protected set; }
    public string searchableName { get; protected set; }
    public string imageUrl { get; protected set; }
    public string bigImageUrl { get; protected set; }

    public static readonly string[] PercentageColumns = [
        "Percentage of Points Scored",
        "Percentage",
        "Percentage of Points Scored For Team",
        "Percentage of Points Served Won",
        "Serve Return Rate",
        "Serve Fault Rate",
        "Serve Ace Rate",
        "Percentage of Rounds Carded",
        "Percentage of Games Started Left"
    ];

    public Dictionary<string, dynamic?>? stats { get; protected set; }


    public PersonData(Person person, Tournament? tournament = null, bool generateStats = false, Team? team = null,
        bool format = false) {
        name = person.Name;
        searchableName = person.SearchableName;
        imageUrl = imageUrl = Utilities.FixImageUrl(person.ImageUrl);
        bigImageUrl = Utilities.FixImageUrl(person.ImageUrl); //TODO: fix this later

        if (!generateStats) return;

        stats = new Dictionary<string, dynamic?> {
            {"B&F Votes", 0.0},
            {"Elo", 0.0},
            {"Games Won", 0.0},
            {"Games Lost", 0.0},
            {"Games Played", 0.0},
            {"Games Started Left", 0.0},
            {"Games Started Right", 0.0},
            {"Games Started Substitute", 0.0},
            {"Caps", 0.0},
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
            {"Votes per 100 games", 0.0}
        };
        var teamPoints = 0;
        var servedPointsWon = 0;
        foreach (var pgs in (person.PlayerGameStats ?? []).OrderBy(pgs => pgs.GameId)) {
            if (tournament != null && pgs.TournamentId != tournament.Id) continue;
            if (team != null && pgs.TeamId != team.Id) continue;
            if (pgs.RoundsCarded + pgs.RoundsOnCourt == 0) continue;
            if (pgs.Game.IsBye) continue;
            var game = pgs.Game;
            stats["Caps"] += game.Ended && game.TournamentId != 1 ? 1 : 0;
            if (!pgs.Game.Ranked && tournament?.Ranked != false) continue;


            if (pgs.Game.IsFinal) continue;
            servedPointsWon += pgs.ServedPointsWon;
            teamPoints += game.TeamOneId == pgs.TeamId ? game.TeamOneScore : game.TeamTwoScore;

            stats["B&F Votes"] += pgs.BestPlayerVotes;
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
            switch (pgs.StartSide) {
                case "Left":
                    stats["Games Started Left"] += 1;
                    break;
                case "Right":
                    stats["Games Started Right"] += 1;
                    break;
                default:
                    stats["Games Started Substitute"] += 1;
                    break;
            }
        }

        stats["Elo"] = person.Elo(tournamentId: tournament?.Id);
        var gamesPlayed = stats["Games Played"];
        stats["Percentage"] = stats["Games Won"] / stats["Games Played"];
        stats["Points per Game"] = stats["Points Scored"] / gamesPlayed;
        stats["Points per Loss"] = stats["Points Scored"] / stats["Games Lost"];
        stats["Aces per Game"] = stats["Aces Scored"] / gamesPlayed;
        stats["Faults per Game"] = stats["Faults"] / gamesPlayed;
        stats["Cards per Game"] = stats["Cards"] / gamesPlayed;
        stats["Points per Card"] = stats["Points Scored"] / stats["Cards"];
        stats["Serves per Game"] = stats["Points Served"] / gamesPlayed;
        stats["Serves per Ace"] = stats["Points Served"] / stats["Aces Scored"];
        stats["Serves per Fault"] = stats["Points Served"] / stats["Faults"];
        stats["Serve Ace Rate"] = stats["Aces Scored"] / stats["Points Served"];
        stats["Serve Fault Rate"] = stats["Faults"] / stats["Points Served"];
        stats["Percentage of Points Scored"] =
            stats["Points Scored"] / Math.Max(stats["Rounds on Court"], 1);
        stats["Percentage of Points Scored For Team"] = stats["Points Scored"] / Math.Max(teamPoints, 1);
        stats["Percentage of Games Started Left"] = stats["Games Started Left"] / gamesPlayed;
        stats["Percentage of Served Points Won"] =
            servedPointsWon / Math.Max(stats["Points Served"], 1);
        stats["Serve Return Rate"] = stats["Serves Returned"] / Math.Max(stats["Serves Received"], 1);
        stats["Votes per 100 Games"] = 100.0f * stats["B&F Votes"] / gamesPlayed;
        stats["Percentage of Rounds Carded"] =
            stats["Rounds Carded"] / (stats["Rounds on Court"] + stats["Rounds Carded"]);
        stats["Rounds Per Game"] = stats["Rounds on Court"] / gamesPlayed;
        if (!format) return;

        FormatData();
    }

    protected void FormatData() {
        foreach (var stat in stats.Keys) {
            if (stats[stat] == null) {
                stats[stat] = "-";
                continue;
            }

            if (double.IsNaN(stats[stat])) stats[stat] = "-";
            else if (PercentageColumns.Contains(stat)) {
                if (double.IsInfinity(stats[stat])) stats[stat] = "\u221e%";
                else stats[stat] = stats[stat].ToString("P2");
            } else {
                if (double.IsInfinity(stats[stat])) stats[stat] = "\u221e";
                else stats[stat] = Math.Round((double) stats[stat], 2);
            }
        }
    }
}