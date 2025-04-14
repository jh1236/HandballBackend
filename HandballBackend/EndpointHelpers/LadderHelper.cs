using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.EndpointHelpers;

public static class LadderHelper {
    public static (TeamData[]?, TeamData[]?, TeamData[]?)
        GetTournamentLadder(HandballContext db, Tournament tournament) {
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
        var ladder = SortTeams(ladderTt.Select(tt => tt.ToSendableData(true)).ToArray());
        var poolOne = SortTeams(poolOneTt.Select(tt => tt.ToSendableData(true)).ToArray());
        var poolTwo = SortTeams(poolTwoTt.Select(tt => tt.ToSendableData(true)).ToArray());
        return (ladder.Length > 0 ? ladder : null, poolOne.Length > 0 ? poolOne : null,
            poolTwo.Length > 0 ? poolTwo : null);
    }


    public static TeamData[] SortTeams(TeamData[] teams) {
        return teams
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

    public static TeamData[] SortTeamsNoTournament(TeamData[] teams) {
        return teams
            .OrderByDescending(t => t.stats!["Percentage"])
            .ThenByDescending(t => t.stats!["Games Played"])
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