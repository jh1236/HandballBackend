using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.EndpointHelpers;

public static class LadderHelper {
    public static async Task<(TeamData[]?, TeamData[]?, TeamData[]?)> GetTournamentLadder(HandballContext db,
        Tournament tournament) {
        var innerQuery = await db.TournamentTeams
            .Where(t => t.TournamentId == tournament.Id)
            .Include(t => t.Team.Captain)
            .Include(t => t.Team.NonCaptain)
            .Include(t => t.Team.Substitute)
            .Include(t => t.Team.PlayerGameStats)
            .ThenInclude(pgs => pgs.Game).ToArrayAsync();
        var ladderTt = innerQuery.Where(t => t.Pool == 0).ToArray();
        var poolOneTt = innerQuery.Where(t => t.Pool == 1).ToArray();
        var poolTwoTt = innerQuery.Where(t => t.Pool == 2).ToArray();
        var ladder = SortTeams(ladderTt.Select(tt => tt.ToSendableData(true)).ToArray());
        var poolOne = SortTeams(poolOneTt.Select(tt => tt.ToSendableData(true)).ToArray());
        var poolTwo = SortTeams(poolTwoTt.Select(tt => tt.ToSendableData(true)).ToArray());
        return (ladder.Length > 0 ? ladder : null, poolOne.Length > 0 ? poolOne : null,
            poolTwo.Length > 0 ? poolTwo : null);
    }

    public static async Task<TeamData[]> GetLadder(HandballContext db) {
        var ladderTt = await db.Teams
            // .Where(t => t.Team.TournamentTeams.Any(tt => tt.TournamentId != 1))
            .Include(t => t.Captain)
            .Include(t => t.NonCaptain)
            .Include(t => t.Substitute)
            .Include(t => t.PlayerGameStats)
            .ThenInclude(pgs => pgs.Game).ToArrayAsync();
        var ladder = SortTeamsNoTournament(ladderTt.Select(t => t.ToSendableData(true)).ToArray());
        return ladder;
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
            .ThenBy(t => t.Elo)
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