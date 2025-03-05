// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using System.Drawing;
using HandballBackend.Database.Models;
using HandballBackend.Models;

namespace HandballBackend.Database.SendableTypes;

public class TeamData {
    public string name { get; private set; }
    public string searchableName { get; private set; }
    public string? imageUrl { get; private set; }
    public string? bigImageUrl { get; private set; }
    public PersonData? captain { get; private set; }
    public PersonData? nonCaptain { get; private set; }
    public PersonData? substitute { get; private set; }
    public string? teamColor { get; private set; }
    public int[]? teamColorAsRGBABecauseDigbyIsLazy { get; private set; }
    public float elo { get; private set; }

    public Dictionary<string, dynamic>? stats { get; private set; }

    private static int[] GenerateRgba(string backgroundColor) {
        var color = ColorTranslator.FromHtml(backgroundColor);
        int r = Convert.ToInt16(color.R);
        int g = Convert.ToInt16(color.G);
        int b = Convert.ToInt16(color.B);
        return [r, g, b, 255];
    }

    private static readonly string[] PercentageColumns = [
        "Percentage"
    ];

    public TeamData(Team team, Tournament? tournament = null, bool generateStats = false,
        bool generatePlayerStats = false, bool formatData = false) {
        name = team.Name;
        searchableName = team.SearchableName;
        imageUrl = team.ImageUrl;
        bigImageUrl = team.BigImageUrl;
        captain = team.Captain?.ToSendableData(tournament, generateStats && generatePlayerStats, team);
        nonCaptain = team.NonCaptain?.ToSendableData(tournament, generateStats && generatePlayerStats, team);
        substitute = team.Substitute?.ToSendableData(tournament, generateStats && generatePlayerStats, team);
        teamColor = team.TeamColor;
        teamColorAsRGBABecauseDigbyIsLazy = teamColor != null ? GenerateRgba(teamColor) : null;
        elo = 1500.0f;

        if (!generateStats) return;

        stats = new Dictionary<string, dynamic> {
            {"Games Played", 0.0},
            {"Games Won", 0.0},
            {"Games Lost", 0.0},
            {"Timeouts Called", 0.0},
            {"Points Scored", 0.0},
            {"Points Against", 0.0},
            {"Green Cards", 0.0},
            {"Yellow Cards", 0.0},
            {"Red Cards", 0.0},
            {"Faults", 0.0},
            {"Double Faults", 0.0},
        };
        var gameId = 0;
        foreach (var pgs in team.PlayerGameStats.Where(pgs =>
                     pgs.Game.Ranked && (tournament == null || pgs.TournamentId == tournament.Id)) ?? []) {
            if (gameId < pgs.GameId) {
                gameId = pgs.GameId;
                stats["Games Played"] += pgs.Game.Ended ? 1 : 0;
                stats["Games Won"] += pgs.Game.Ended && pgs.Game.WinningTeamId == team.Id ? 1 : 0;
                stats["Games Lost"] += pgs.Game.Ended && pgs.Game.WinningTeamId != team.Id ? 1 : 0;
                stats["Timeouts Called"] +=
                    (pgs.Game.TeamOneId == team.Id ? pgs.Game.TeamOneTimeouts : pgs.Game.TeamTwoTimeouts);
                stats["Points Against"] =
                    (pgs.Game.TeamOneId == team.Id ? pgs.Game.TeamTwoScore : pgs.Game.TeamOneScore);
            }

            stats["Green Cards"] += pgs.GreenCards;
            stats["Yellow Cards"] += pgs.YellowCards;
            stats["Red Cards"] += pgs.RedCards;
            stats["Faults"] += pgs.Faults;
            stats["Double Faults"] += pgs.DoubleFaults;
            stats["Points Scored"] += pgs.PointsScored;
        }

        stats["Point Difference"] = stats["Points Scored"] - stats["Points Against"];
        stats["Percentage"] = stats["Games Won"] / Math.Max(stats["Games Played"], 1);

        if (!formatData) return;

        FormatData();
    }

    public void FormatData() {
        foreach (var stat in stats.Keys) {
            if (stats[stat] == null) continue;
            if (PercentageColumns.Contains(stat)) {
                stats[stat] = 100.0 * Math.Round((double) stats[stat], 2) + "%";
            }
            else {
                stats[stat] = Math.Round((double) stats[stat], 2).ToString();
            }
        }
    }
}