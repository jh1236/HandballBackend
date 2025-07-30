using HandballBackend.EndpointHelpers;
using HandballBackend.Controllers;
using HandballBackend.Database;
using HandballBackend.Database.Models;
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

    internal class UmpiringSolution {
        public required int GameId;
        public required int CourtId;
        public required List<int> PlayerIds;
        public int? OfficialId;
        public int? ScorerId;
    }

    private async Task AddUmpires() {
        var db = new HandballContext();
        var games = await db.Games.Where(g => g.TournamentId == tournamentId && !g.Started && !g.IsBye)
            .IncludeRelevant().ToListAsync();
        var courtOneGames = games.Where(g => g.Court == 0).ToList();
        var courtTwoGames = games.Where(g => g.Court == 1).Cast<Game?>().ToList();
        var officials = (await db.TournamentOfficials.Where(to => to.TournamentId == tournamentId)
                .Include(to => to.Official).GroupBy(to => to.Official.Proficiency)
                .OrderBy(g => g.Key).ToListAsync())
            .Select(g => g.Select(to => to.Official.Id).ToList())
            .ToList();

        while (courtOneGames.Count > courtTwoGames.Count) {
            courtTwoGames.Add(null);
        }

        var solution = new List<(UmpiringSolution, UmpiringSolution?)>();
        foreach (var (courtOne, courtTwo) in courtOneGames.Zip(courtTwoGames)) {
            var courtOneGame = new UmpiringSolution {
                GameId = courtOne.Id,
                CourtId = 0,
                PlayerIds = courtOne.TeamOne.People.Concat(courtOne.TeamTwo.People).Select(p => p.Id).ToList(),
            };
            var courtTwoGame = courtTwo == null
                ? null
                : new UmpiringSolution {
                    GameId = courtTwo.Id,
                    CourtId = 1,
                    PlayerIds = courtTwo.TeamOne.People.Concat(courtTwo.TeamTwo.People).Select(p => p.Id).ToList(),
                };
            solution.Add(new(courtOneGame, courtTwoGame));
        }

        var solutionArray = solution.ToArray();
        TrySolution(solutionArray, 0, true, officials);
    }

    internal bool TrySolution((UmpiringSolution, UmpiringSolution?)[] solutions, int index, bool courtOne,
        List<List<int>> officialsByProficiency) {
        var (myGame, otherGame) = solutions[index];
        if (!courtOne) {
            (myGame, otherGame) = (otherGame, myGame);
        }

        if (myGame == null) {
            return TrySolution(solutions, index + (courtOne ? 0 : 1), !courtOne, officialsByProficiency);
        }

        List<List<int>> officialsByPreference = [..officialsByProficiency];
        if (!courtOne) {
            // we are on court two
            officialsByPreference.Reverse();
        }

        foreach (var officials in officialsByPreference) {
            foreach (var official in officials) {
                if (myGame.PlayerIds.Contains(official)) continue;
                if (otherGame?.PlayerIds.Contains(official) ?? false) continue;
                if (otherGame?.OfficialId == official) continue;

                myGame.OfficialId = official;
                var success = TrySolution(solutions, index + (courtOne ? 0 : 1), !courtOne, officialsByProficiency);

                if (!success) {
                    myGame.OfficialId = null;
                }
            }
        }

        return false;
    }
}