using HandballBackend.EndpointHelpers;
using System.Runtime.CompilerServices;
using HandballBackend.Controllers;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using Microsoft.EntityFrameworkCore;

[assembly: InternalsVisibleTo("HandballBackend.Tests")]

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

    public virtual async Task BeginTournament() {
        var db = new HandballContext();
        (await db.Tournaments.FindAsync(tournamentId))!.Started = true;
        await EndOfRound();
        await db.SaveChangesAsync();
    }


    internal async Task AddCourts(int rounds = -1) {
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
        public OfficialContainer? Official;
        public OfficialContainer? Scorer;
    }

    internal class OfficialContainer {
        public string Name;
        public int PlayerId;
        public int OfficialId;
        public int GamesUmpired;
        public int GamesScored;
        public int UmpireProficiency;
        public int ScorerProficiency;
    }

    private async Task AddUmpires() {
        var db = new HandballContext();
        var games = await db.Games.Where(g => g.TournamentId == tournamentId && !g.Started && !g.IsBye)
            .IncludeRelevant().ToListAsync();
        if (games.Count <= 0) return;
        var round = games.Max(g => g.Round);
        var courtOneGames = games.Where(g => g.Court == 0).ToList();
        var courtTwoGames = games.Where(g => g.Court == 1).Cast<Game?>().ToList();
        var officials =
            db.TournamentOfficials
                .Where(to => to.TournamentId == tournamentId)
                .Include(to => to.Official.Person)
                .Include(to => to.Official.Games.Where(g => g.TournamentId == tournamentId && g.Round < round))
                .Include(to =>
                    to.Official.ScoredGames.Where(g => g.TournamentId == tournamentId && g.Round < round))
                .ToList()
                .Select(to => new OfficialContainer {
                    PlayerId = to.Official.PersonId,
                    OfficialId = to.OfficialId,
                    GamesUmpired = to.Official.Games.Count(g => g.TournamentId == tournamentId && g.Round < round),
                    Name = to.Official.Person.Name,
                    GamesScored = to.Official.ScoredGames.Count(g => g.TournamentId == tournamentId && g.Round < round),
                    UmpireProficiency = to.UmpireProficiency,
                    ScorerProficiency = to.ScorerProficiency
                }).OrderBy(o => o.GamesUmpired).ToList();

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
        if (!TrySolution(solutionArray, officials)) {
            //the solution found no possible result
            TrySolution(solutionArray, officials, 0, true, false, true);
        }
    }

    internal static bool TrySolution((UmpiringSolution, UmpiringSolution?)[] solutions,
        List<OfficialContainer> officials,
        int index = 0,
        bool courtOne = true,
        bool scorer = false,
        bool force = false) {
        if (index >= solutions.Length) {
            if (scorer) return true;
            // we have reached the last game
            if (!TrySolution(solutions, officials, 0, true, true)) {
                return TrySolution(solutions, officials, 0, true, true, true);
            }

            return true;
        }

        var (myGame, otherGame) = solutions[index];
        if (!courtOne) {
            (myGame, otherGame) = (otherGame, myGame);
        }

        if (myGame == null) {
            return TrySolution(solutions, officials, index + (courtOne ? 0 : 1), !courtOne, scorer);
        }

        var officialsByPreference = officials
            .Where(to => (scorer ? to.ScorerProficiency : to.UmpireProficiency) != 0)
            .GroupBy(to => scorer ? to.ScorerProficiency : to.UmpireProficiency)
            .OrderByDescending(k => k.Key)
            .Select(to => to.ToList())
            .ToList();

        if (!courtOne && !scorer) {
            officialsByPreference.Reverse();
        }

        foreach (var officialsList in officialsByPreference) {
            foreach (var official in officialsList.OrderBy(o => scorer ? o.GamesScored : o.GamesUmpired)
                         .ThenBy(o => scorer ? o.GamesUmpired : o.GamesScored)) {
                if (myGame.PlayerIds.Contains(official.PlayerId)) continue;
                if (otherGame?.PlayerIds.Contains(official.PlayerId) ?? false) continue;
                if (otherGame?.Official == official) continue;

                if (myGame.Official == official) continue;
                if (otherGame?.Scorer == official) continue;

                if (scorer) {
                    myGame.Scorer = official;
                    official.GamesScored++;
                } else {
                    myGame.Official = official;
                    official.GamesUmpired++;
                }

                var success = TrySolution(solutions,
                    officials, index + (courtOne ? 0 : 1), !courtOne, scorer, force);

                if (success) {
                    return true;
                }

                if (scorer) {
                    myGame.Scorer = null;
                    official.GamesScored--;
                } else {
                    myGame.Official = null;
                    official.GamesUmpired--;
                }
            }
        }

        if (force) {
            if (scorer) {
                var official = myGame.Official!;

                myGame.Scorer = official;
                official.GamesScored++;
            } else {
                var official = new OfficialContainer() {
                    GamesScored = 0,
                    GamesUmpired = 0,
                    Name = "Unfillable",
                    OfficialId = -1,
                    PlayerId = -1,
                };

                myGame.Official = official;
            }

            return TrySolution(solutions,
                officials, index + (courtOne ? 0 : 1), !courtOne, scorer, force);
        }

        return false;
    }
}