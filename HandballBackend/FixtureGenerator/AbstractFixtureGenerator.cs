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
        // Get all necessary data
        var db = new HandballContext();
        var gamesQuery = await db.Games
            .Where(g => g.TournamentId == tournamentId && !g.IsBye)
            .OrderBy(g => g.Id)
            .ToListAsync();

        var players = await db.PlayerGameStats
            .Join(db.Games,
                pgs => pgs.GameId,
                g => g.Id,
                (pgs, g) => new { pgs, g })
            .Where(x => x.pgs.TournamentId == tournamentId && !x.g.IsBye)
            .Select(x => x.pgs)
            .ToListAsync();

        var tourney = await db.Tournaments
            .FirstOrDefaultAsync(t => t.Id == tournamentId);

        var officials = await db.TournamentOfficials
            .Where(to => to.TournamentId == tournamentId)
            .Include(to => to.Official)
            .ToListAsync();

        // Organize data
        var rounds = gamesQuery
            .GroupBy(g => g.Round)
            .Select(g => g.ToList())
            .ToList();

        var gameToPlayers = players
            .GroupBy(p => p.GameId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process each round
        foreach (var round in rounds) {
            var courtOneGames = round.Where(g => g.Court == 0).ToList();
            var courtTwoGames = round.Where(g => g.Court == 1).ToList();

            // Pair games from both courts
            var maxCount = Math.Max(courtOneGames.Count, courtTwoGames.Count);
            for (var i = 0; i < maxCount; i++) {
                var game1 = i < courtOneGames.Count ? courtOneGames[i] : null;
                var game2 = i < courtTwoGames.Count ? courtTwoGames[i] : null;
                var games = new[] { game1, game2 }.Where(g => g != null).ToList();

                // Assign umpires
                foreach (var game in games) {
                    if (game is not { OfficialId: null })
                        continue;

                    var officialsForCourt = game.Court == 0
                        ? officials.OrderByDescending(o => o.Official.Proficiency)
                            .ThenBy(o => o.GamesUmpired)
                            .ThenBy(o => o.CourtOneGamesUmpired)
                            .ToList()
                        : officials.OrderBy(o => o.Official.Proficiency == 0 ? 3 : o.Official.Proficiency)
                            .ThenBy(o => o.GamesUmpired)
                            .ThenByDescending(o => o.CourtOneGamesUmpired)
                            .ToList();

                    foreach (var official in officialsForCourt) {
                        // Check if official is already umpiring this round
                        if (games.Any(g => g != null && g.OfficialId == official.OfficialId))
                            continue;

                        // Check if official is playing this round
                        if (games.Any(g => g != null &&
                                           gameToPlayers.ContainsKey(g.Id) &&
                                           gameToPlayers[g.Id].Any(p => p.PlayerId == official.Official.PersonId)))
                            continue;

                        game.OfficialId = official.OfficialId;
                        await db.SaveChangesAsync();
                        break;
                    }
                }
            }

            // Assign scorers if tournament has them
            if (!tourney!.HasScorer)
                continue;

            // Pair games again for scorer assignment
            for (var i = 0; i < maxCount; i++) {
                var game1 = i < courtOneGames.Count ? courtOneGames[i] : null;
                var game2 = i < courtTwoGames.Count ? courtTwoGames[i] : null;
                var games = new[] { game1, game2 }.Where(g => g != null).ToList();

                foreach (var game in games) {
                    if (game is not { ScorerId: null })
                        continue;

                    var scorerCandidates = officials
                        .OrderBy(o => o.Official.Proficiency == 0)
                        .ThenBy(o => o.GamesUmpired)
                        .ThenBy(o => o.GamesScored)
                        .ToList();

                    foreach (var official in scorerCandidates) {
                        // Check if official is umpiring this round
                        if (games.Any(g => g != null && g.OfficialId == official.OfficialId))
                            continue;

                        // Check if official is already scoring this round
                        if (games.Any(g => g != null && g.ScorerId == official.OfficialId))
                            continue;

                        // Check if official is playing this round
                        if (games.Any(g => g != null &&
                                           gameToPlayers.ContainsKey(g.Id) &&
                                           gameToPlayers[g.Id].Any(p => p.PlayerId == official.Official.PersonId)))
                            continue;

                        game.ScorerId = official.OfficialId;
                        await db.SaveChangesAsync();
                        break;
                    }

                    // If no scorer found, set to umpire
                    if (game.ScorerId == null && game.OfficialId != null) {
                        game.ScorerId = game.OfficialId;
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        db.SaveChanges();
    }
}