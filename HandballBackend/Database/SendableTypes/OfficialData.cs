// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions


using HandballBackend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.SendableTypes;

public class OfficialData : PersonData {
    public Dictionary<string, float> stats { get; private set; }

    public OfficialData(Official official, bool includeStats = false) : base(official.Person) {
        if (!includeStats) return;
        var db = new HandballContext();
        var playerGameStats = db.PlayerGameStats
            .Where(g => g.Game.OfficialId == official.Id || g.Game.ScorerId == official.Id)
            .Include(g => g.Game)
            .OrderBy(g => g.GameId);
        var prevGameId = 0;
        stats = new Dictionary<string, float>();
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