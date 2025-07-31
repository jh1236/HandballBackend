using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.FixtureGenerator;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HandballBackend.Tests.FixtureGenerator;

[TestClass]
[TestSubject(typeof(AbstractFixtureGenerator))]
public class AbstractFixtureGeneratorTest {
    [ClassInitialize]
    public static void TestInitialize(TestContext testContext) {
        Config.USING_POSTGRES = true;
        Config.SECRETS_FOLDER = @"..\HandballBackend\build\secrets\";
    }

    [ClassCleanup]
    public static void TestCleanup() {
        Config.USING_POSTGRES = false;
        Config.SECRETS_FOLDER = @".\Config\Secrets\";
    }

    [TestMethod]
    public async Task TestUmpireAssignment() {
        const int tournamentId = 11;
        const int round = 1;
        var db = new HandballContext();
        var games = await db.Games.Where(g => g.TournamentId == tournamentId && g.Round == round)
            .IncludeRelevant().ToListAsync();
        var courtOneGames = games.Where(g => g.Court == 0).ToList();
        var courtTwoGames = games.Where(g => g.Court == 1).Cast<Game?>().ToList();
        var officials =
            (await db.TournamentOfficials
                .Where(to => to.TournamentId == tournamentId)
                .Include(to => to.Official.Person)
                .Include(to => to.Official.Games.Where(g => g.TournamentId == tournamentId && g.Round < round))
                .Include(to => to.Official.ScoredGames.Where(g => g.TournamentId == tournamentId && g.Round < round))
                .ToListAsync())
            .Select(to => new AbstractFixtureGenerator.OfficialContainer {
                PlayerId = to.Official.PersonId,
                OfficialId = to.OfficialId,
                GamesUmpired = to.Official.Games.Count,
                Name = to.Official.Person.Name,
                GamesScored = to.Official.ScoredGames.Count,
                Proficiency = to.Official.Proficiency,
            }).OrderBy(o => o.GamesUmpired).ToList();
        while (courtOneGames.Count > courtTwoGames.Count) {
            courtTwoGames.Add(null);
        }

        var solution =
            new List<(AbstractFixtureGenerator.UmpiringSolution, AbstractFixtureGenerator.UmpiringSolution?)>();
        foreach (var (courtOne, courtTwo) in courtOneGames.Zip(courtTwoGames)) {
            var courtOneGame = new AbstractFixtureGenerator.UmpiringSolution {
                GameId = courtOne.Id,
                CourtId = 0,
                PlayerIds = courtOne.TeamOne.People.Concat(courtOne.TeamTwo.People).Select(p => p.Id).ToList(),
            };
            var courtTwoGame = courtTwo == null
                ? null
                : new AbstractFixtureGenerator.UmpiringSolution {
                    GameId = courtTwo.Id,
                    CourtId = 1,
                    PlayerIds = courtTwo.TeamOne.People.Concat(courtTwo.TeamTwo.People).Select(p => p.Id).ToList(),
                };
            solution.Add(new(courtOneGame, courtTwoGame));
        }

        var solutionArray = solution.ToArray();
        Console.WriteLine("All Umpires:");
        foreach (var officialList in officials.GroupBy(o => o.Proficiency)) {
            Console.WriteLine($"Proficiency: {officialList.Key}");
            Console.WriteLine(
                $"{string.Join("\n\t", officialList.Select(o => $"{o.Name} : {o.GamesUmpired}, {o.GamesScored}"))}");
        }

        Console.WriteLine("--------------------");
        Console.WriteLine($"Success: {AbstractFixtureGenerator.TrySolution(solutionArray, officials, force: true)}");
        foreach (var game in solutionArray.SelectMany(g => new[] {g.Item1, g.Item2})) {
            if (game == null) continue;
            Console.WriteLine($"Game {game.GameId} on Court {game.CourtId + 1}");
            Console.WriteLine($"\tPlayers: {string.Join(", ", game.PlayerIds)}");
            Console.WriteLine($"\tUmpire: {game.Official?.PlayerId}");
            Console.WriteLine($"\tScorer: {game.Scorer?.PlayerId}");
        }

        Console.WriteLine("--------------------");
        Console.WriteLine("All Umpires:");
        Console.WriteLine("--------------------");
        foreach (var officialList in officials.GroupBy(o => o.Proficiency)) {
            Console.WriteLine($"Proficiency: {officialList.Key}");
            Console.WriteLine(
                $"{string.Join("\n\t", officialList.Select(o => $"{o.Name} : {o.GamesUmpired}, {o.GamesScored}"))}");
        }
    }
}