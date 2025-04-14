// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.SendableTypes;

public class OfficialData : PersonData {
    public new Dictionary<string, float> stats { get; private set; }

    public OfficialData(Official official, Tournament? tournament = null, bool includeStats = false) : base(
        official.Person) {
        if (!includeStats) return;

        var playerGameStats =
            official.Games
                .Where(g => tournament == null || g.TournamentId == tournament.Id)
                .OrderBy(g => g.Id).SelectMany(g => g.Players);
        var prevGameId = 0;
        stats = new Dictionary<string, float> {
            {"Games Umpired", 0},
            {"Rounds Umpired", 0},
            {"Green Cards Given", 0},
            {"Yellow Cards Given", 0},
            {"Red Cards Given", 0},
            {"Cards Given", 0},
            {"Faults Called", 0},
            {"Double Faults Called", 0},
        };
        foreach (var pgs in playerGameStats) {
            if (pgs.GameId > prevGameId) {
                prevGameId = pgs.GameId;
                stats["Games Umpired"] += 1;
                stats["Rounds Umpired"] += pgs.Game.TeamOneScore + pgs.Game.TeamTwoScore;
            }

            stats["Green Cards Given"] += pgs.GreenCards;
            stats["Yellow Cards Given"] += pgs.YellowCards;
            stats["Red Cards Given"] += pgs.RedCards;
            stats["Cards Given"] += pgs.GreenCards + pgs.YellowCards + pgs.RedCards;
            stats["Faults Called"] += pgs.Faults;
            stats["Double Faults Called"] += pgs.DoubleFaults;
        }
    }
}