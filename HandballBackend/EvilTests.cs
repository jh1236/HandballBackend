using System.Text;
using System.Text.Unicode;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.EndpointHelpers.GameManagement;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend;

internal static class EvilTests {
    public static void init() {
        Config.SECRETS_FOLDER = @"G:\Programming\c#\HandballBackend\HandballBackend\secrets";
    }


    public static void EvilTest() {
        init();
        var db = new HandballContext();
        var gE = db.GameEvents.OrderByDescending(gE => gE.Id).First();
    }

    public static void MalevolantTest() {
        init();
        var teamElos = new Dictionary<int, double>();
        var db = new HandballContext();
        var games = db.Games.Where(g => !g.IsFinal && g.Ranked && !g.IsBye)
            .OrderBy(g => g.TournamentId == 2)
            .ThenBy(g => g.TournamentId == 3)
            .ThenBy(g => g.StartTime)
            .ThenBy(g => g.Id)
            .IncludeRelevant().Include(g => g.Events.Where(gE => gE.EventType == GameEventType.Forfeit));
        foreach (var game in games) {
            Console.WriteLine($"Game {game.TeamOne.Name} vs {game.TeamTwo.Name}");
            var playingPlayers = game.Players
                .Where(pgs =>
                    (game.Events.Any(gE => gE.EventType == GameEventType.Forfeit) ||
                     pgs.RoundsCarded + pgs.RoundsOnCourt > 0)).ToList();
            var teamOneElo = teamElos.GetValueOrDefault(game.TeamOne.Id, playingPlayers
                .Where(pgs => pgs.TeamId == game.TeamOneId).Select(pgs => pgs.InitialElo)
                .Average());
            var teamTwoElo = teamElos.GetValueOrDefault(game.TeamTwo.Id, playingPlayers
                .Where(pgs => pgs.TeamId == game.TeamTwoId).Select(pgs => pgs.InitialElo)
                .Average());
            foreach (var pgs in game.Players) {
                var myElo = pgs.TeamId == game.TeamOneId ? teamOneElo : teamTwoElo;
                var oppElo = pgs.TeamId == game.TeamOneId ? teamTwoElo : teamOneElo;
                var eloDelta = EloCalculator.CalculateEloDelta(myElo, oppElo, game.WinningTeamId == pgs.TeamId);
                pgs.EloDelta = eloDelta;
                teamElos[pgs.TeamId] = myElo + eloDelta;
            }
        }

        db.SaveChanges();
    }

    public static void SinisterTest() {
        init();
        var teams = new[] {
            62,
            126,
            116,
            127,
            128,
            129,
            131,
            132,
            133,
            134,
            136
        };

        var officials = new[] {1, 2, 6, 11, 14, 15, 17};
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

    public static void WickedTest() {
        init();
        return;
        var db = new HandballContext();
        var people = db.TournamentTeams.Where(tt => tt.TournamentId == 10).IncludeRelevant().Select(t => t.Team)
            .ToArray()
            .SelectMany(t => t.People).ToList();
        var tasks = new List<Task>();
        foreach (var p in people) {
            Console.WriteLine($"Texting {p.Name}");
            tasks.Add(TextHelper.Text(p,
                //$"Hi {p.Name.Split(" ")[0]}!\n  Just a reminder that the 9th SUSS Championship is on at 5pm today at Manning Library (2 Conochie Cres). Don't forget to bring a jumper as it is set to get quite cold!\n\nThanks, and as always, Happy Balling!")
                "Hi all! It appears that I have accidentally pressed send on a group text.  Please Disregard that last message. Thanks, and happy balling!")
            );
        }

        Task.WaitAll(tasks.ToArray());
    }

    public static void Test() {
        init();
        var db = new HandballContext();
        db.People.First(p => p.SearchableName == "remy_mcgunnigle").PhoneNumber = "+61447125557";
        db.SaveChanges();
    }

    
public static void ListPhoneNumbers() {
        init();
        var db = new HandballContext();
        var people = db.People;
        foreach (var p in people) {
            Console.WriteLine($"{p.PhoneNumber} - {p.Name}");
        }
    }
public static void VillanousTest() {
        init();
        var db = new HandballContext();
        var people = db.People.OrderBy(p => p.SearchableName == "kaliha_bhuiyan" ? 0 : 1);
        var taskList = new List<Task>();
        foreach (var p in people) {
            if (p.PhoneNumber == null) continue;
            Console.WriteLine($"Should {p.Name} be texted?");
            var shouldBeTexted = Console.ReadLine()?.ToLower() == "y";
            if (!shouldBeTexted) continue;
            taskList.Add(TextHelper.Text(p, $"Hi {p.Name.Split(" ")[0]}, " +
                                            $"\n You have been invited to the 10th Squarers' United Sporting Syndicate Handball Championship. This event will be hosted at 5pm on the 13th of July. The event will be located at the Manning Library (2 Conochie Cr.). Please respond YES if you are available, or NO if you are not." +
                                            $"\nThanks, and happy balling!"));
        }
    }
}