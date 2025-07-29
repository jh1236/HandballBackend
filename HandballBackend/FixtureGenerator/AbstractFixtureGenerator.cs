using HandballBackend.EndpointHelpers;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.FixtureGenerator;

public abstract class AbstractFixtureGenerator(int tournamentId, bool fillOfficials, bool fillCourts) {
    public static AbstractFixtureGenerator GetControllerByName(string name, int tournamentId) {
        return name switch {
            "OneRound" => new OneRound(tournamentId),
            "Pooled" => new Pooled(tournamentId),
            "RoundRobin" => new RoundRobin(tournamentId),
            "Swiss" => new Swiss(tournamentId),
            "PooledFinals" => new PooledFinals(tournamentId),
            "BasicFinals" => new BasicFinals(tournamentId),
            "TopThreeFinals" => new TopThreeFinals(tournamentId),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
        };
    }

    public virtual async Task<bool> EndOfRound() {
        if (fillCourts) {
            await AddCourts();
        }

        if (fillOfficials) {
            await AddUmpires();
        }

        return false;
    }

    protected void EndTournament(
        string note = "Thank you for participating in the tournament! We look forward to seeing you next time.") {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(tournamentId)!;
        tournament.Finished = true;
        tournament.Notes = note;
        db.SaveChanges();
        _ = Task.Run(() => PostgresBackup.MakeTimestampedBackup("Post Tournament Backup"));
    }

    public virtual void BeginTournament() {
        var db = new HandballContext();
        db.Tournaments.Find(tournamentId)!.Started = true;
        EndOfRound();
        db.SaveChanges();
    }


    public async Task AddCourts(int rounds = -1) {
        var db = new HandballContext();
        // Get the highest round number

        if (rounds == -1) {
            rounds = (await db.Games
                .Where(g => g.TournamentId == tournamentId)
                .OrderByDescending(g => g.Round)
                .FirstOrDefaultAsync())?.Round ?? 0;
        }

        var games = await db.Games
            .Where(g => g.TournamentId == tournamentId &&
                        g.Round == rounds &&
                        !g.IsBye &&
                        !g.Started &&
                        !g.IsFinal)
            .Include(g => g.TeamOne.PlayerGameStats.Where(pgs => pgs.Game.TournamentId == tournamentId))
            .ThenInclude(pgs => pgs.Game)
            .Include(g => g.TeamTwo.PlayerGameStats.Where(pgs => pgs.Game.TournamentId == tournamentId))
            .ThenInclude(pgs => pgs.Game)
            .ToListAsync();
        var tourney = db.Tournaments
            .FirstOrDefault(t => t.Id == tournamentId);

        var finals = await db.Games
            .Where(g => g.TournamentId == tournamentId &&
                        g.Round == rounds &&
                        !g.IsBye &&
                        !g.Started &&
                        g.IsFinal)
            .ToListAsync();

        // Calculate the split point
        var splitPoint = (int) Math.Ceiling(games.Count / 2.0) - 1;

        // Sort games by combined wins of both teams
        games = games.OrderByDescending(g =>
                g.TeamOne.ToSendableData(true, tournament: tourney).Stats!["Games Won"] +
                g.TeamTwo.ToSendableData(true, tournament: tourney).Stats!["Games Won"])
            .ToList();

        // Assign courts (0 for first half, 1 for second half)
        for (var i = 0; i < games.Count; i++) {
            games[i].Court = i > splitPoint ? 1 : 0;
        }

        // All finals go to court 0
        foreach (var final in finals) {
            final.Court = 0;
        }

        await db.SaveChangesAsync();
    }

    private async Task AddUmpires() {

    }
}