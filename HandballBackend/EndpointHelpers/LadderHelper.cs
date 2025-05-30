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
            .OrderByDescending(t => t.Stats!["Percentage"])
            .ThenByDescending(t => t.Stats!["Point Difference"])
            .ThenByDescending(t => t.Stats!["Points Scored"])
            .ThenBy(t => t.Stats!["Red Cards"])
            .ThenBy(t => t.Stats!["Yellow Cards"])
            .ThenBy(t => t.Stats!["Green Cards"])
            .ThenBy(t => t.Stats!["Double Faults"])
            .ThenBy(t => t.Stats!["Faults"])
            .ThenBy(t => t.Stats!["Timeouts Called"])
            .ToArray();
    }

    public static TeamData[] SortTeamsNoTournament(TeamData[] teams) {
        return teams
            .OrderByDescending(t => t.Stats!["Percentage"])
            .ThenByDescending(t => t.Stats!["Games Played"])
            .ThenByDescending(t => t.Stats!["Point Difference"])
            .ThenByDescending(t => t.Stats!["Points Scored"])
            .ThenBy(t => t.Stats!["Red Cards"])
            .ThenBy(t => t.Stats!["Yellow Cards"])
            .ThenBy(t => t.Stats!["Green Cards"])
            .ThenBy(t => t.Stats!["Double Faults"])
            .ThenBy(t => t.Stats!["Faults"])
            .ThenBy(t => t.Stats!["Timeouts Called"])
            .ToArray();
    }
}