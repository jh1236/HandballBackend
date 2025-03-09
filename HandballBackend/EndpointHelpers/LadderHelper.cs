using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.EndpointHelpers;

public static class LadderHelper {
    public static (TeamData[]?, TeamData[]?, TeamData[]?) GetTournamentLadder(HandballContext db, Tournament tournament) {
        var innerQuery = db.TournamentTeams
            .Where(t => t.TournamentId == tournament.Id)
            .Include(t => t.Team.Captain)
            .Include(t => t.Team.NonCaptain)
            .Include(t => t.Team.Substitute)
            .Include(t => t.Team.PlayerGameStats)
            .ThenInclude(pgs => pgs.Game).ToArray();
        var ladderTt = innerQuery.Where(t => t.Pool == 0).ToArray();
        var poolOneTt = innerQuery.Where(t => t.Pool == 1).ToArray();
        var poolTwoTt = innerQuery.Where(t => t.Pool == 2).ToArray();
        var ladder = SortTeams(tournament, ladderTt.Select(tt => tt.Team).ToArray());
        var poolOne = SortTeams(tournament, poolOneTt.Select(tt => tt.Team).ToArray());
        var poolTwo = SortTeams(tournament, poolTwoTt.Select(tt => tt.Team).ToArray());
        return (ladder.Length > 0 ? ladder : null, poolOne.Length > 0 ? poolOne : null,
            poolTwo.Length > 0 ? poolTwo : null);
    }


    public static TeamData[] SortTeams(Tournament? tournament, Team[] teams) {
        return teams.Select(t => t.ToSendableData(tournament, true))
            .OrderByDescending(t => t.stats!["Percentage"])
            .ThenByDescending(t => t.stats!["Point Difference"])
            .ThenByDescending(t => t.stats!["Points Scored"])
            .ThenBy(t => t.stats!["Red Cards"])
            .ThenBy(t => t.stats!["Yellow Cards"])
            .ThenBy(t => t.stats!["Green Cards"])
            .ThenBy(t => t.stats!["Double Faults"])
            .ThenBy(t => t.stats!["Faults"])
            .ThenBy(t => t.stats!["Timeouts Called"])
            .ToArray();
    }
}