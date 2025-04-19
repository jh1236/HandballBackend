using System.Text;
using System.Text.Unicode;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.EndpointHelpers.GameManagement;
using HandballBackend.Utils;

namespace HandballBackend;

internal static class EvilTests {
    public static void init() {
        HandballContext.DbPath = @"G:\Programming\c#\HandballBackend\HandballBackend\resources\database.db";
    }

    public static void EvilTest() {
        init();
        var db = new HandballContext();
        IQueryable<Team> query = db.Teams;
        query = Team.GetRelevant(query);
        query = query.IncludeRelevant();
        Console.WriteLine(query.ToArray());
    }

    public static void MalevolantTest() {
        init();
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(10);
        for (int i = 0; i < 7; i++) {
            tournament.GetFinalGenerator.AddCourts(i);
        }
    }

    public static void SinisterTest() {
        init();
        var teams = new[] {
            62,
            125,
            126,
            116,
            127,
            128,
            129,
            73,
            130,
            131,
            132,
            133,
            134,
        };

        var officials = new[] {1, 2, 4, 6, 11, 14, 15, 16};
        CreateTournament(
            "The Ninth SUSS Championship",
            "Pooled",
            "PooledFinals",
            true,
            true,
            true,
            true,
            teams,
            officials
        );
    }

    public static Tournament CreateTournament(
        string name,
        string fixturesGen,
        string finalsGen,
        bool ranked,
        bool twoCourts,
        bool scorer,
        bool badmintonServes,
        int[]? teams = null,
        int[]? officials = null) {
        var db = new HandballContext();
        officials ??= [];
        teams ??= [];

        var searchableName = Utilities.ToSearchable(name);

        var tournament = new Tournament {
            Name = name,
            SearchableName = searchableName,
            FixturesType = fixturesGen,
            FinalsType = finalsGen,
            Ranked = ranked,
            TwoCourts = twoCourts,
            HasScorer = scorer,
            BadmintonServes = badmintonServes,
            ImageUrl = $"/api/tournaments/image?name={searchableName}"
        };

        db.Tournaments.Add(tournament);
        db.SaveChanges();

        foreach (var teamId in teams.OrderBy(i => i)) {
            db.TournamentTeams.Add(new TournamentTeam {
                TournamentId = tournament.Id,
                TeamId = teamId
            });
        }

        foreach (var officialId in officials.OrderBy(i => i)) {
            db.TournamentOfficials.Add(new TournamentOfficial {
                TournamentId = tournament.Id,
                OfficialId = officialId,
                IsScorer = true,
                IsUmpire = true
            });
        }

        db.SaveChanges();

        tournament.BeginTournament();

        return tournament;
    }

    public static void MaliciousTest() {
        string? x;
        do {
            Console.WriteLine("Enter the target string");
            x = Console.ReadLine();
            if (x != null && x != "x") {
                Console.WriteLine(EncryptionHelper.Encrypt(x));
            }
        } while (x != "x");
    }

    public static void NefariousTest() {
        init();
        var db = new HandballContext();
        var digby = db.People.Single(p => p.SearchableName == "digby_ross");
        TextHelper.Text(digby, "And now the api key isnt even in the code!!").GetAwaiter().GetResult();
    }

    public static void DeviousTest() {
        init();
        Console.WriteLine("Enter the Lowest game Number");
        var db = new HandballContext();
        var i = int.Parse(Console.ReadLine() ?? string.Empty);
        var game = db.Games.FirstOrDefault(g => g.GameNumber == i);
        while (game != null) {
            Console.WriteLine($"Game {i}");
            if (game.IsBye) {
                i++;
                game = db.Games.FirstOrDefault(g => g.GameNumber == i);
                continue;
            }
            GameManager.StartGame(i, false, null, null, true);
            GameManager.Forfeit(i, false);
            GameManager.End(
                i,
                null,
                3, 3,
                "Testing",
                null,
                null,
                "",
                "", false
            );
            i++;
            game = db.Games.FirstOrDefault(g => g.GameNumber == i);
        }
    }
    
    public static void ConnivingTest() {
        init();
        Console.WriteLine("Enter the Lowest game Number");
        var db = new HandballContext();
        var i = int.Parse(Console.ReadLine() ?? string.Empty);
        var game = db.Games.FirstOrDefault(g => g.GameNumber == i);
        var c = true;
        while (game != null && c) {
            Console.WriteLine($"Game {i}");
            if (game.IsBye) {
                i++;
                game = db.Games.FirstOrDefault(g => g.GameNumber == i);
                continue;
            }
            GameManager.StartGame(i, false, null, null, true);
            GameManager.Forfeit(i, false);
            GameManager.End(
                i,
                null,
                3, 3,
                "Testing",
                null,
                null,
                "",
                "", false
            );
            i++;
            game = db.Games.FirstOrDefault(g => g.GameNumber == i);
            c = Console.ReadLine().ToLower() != "x";
        }
    }
}