using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;

namespace HandballBackend;

internal static class EvilTests {
    private static HandballContext db = new HandballContext(@"G:\Programming\c#\HandballBackend\HandballBackend\resources\database.db");
    
    public static void EvilTest() {
        IQueryable<Team> query = db.Teams;
        query = Team.GetRelevant(query);
        query = query.IncludeRelevant();
        Console.WriteLine(query.ToArray());
    }

    public static void SinisterTest() {
        
    }
    
    public static Tournament CreateTournament(
        string name,
        string fixturesGen,
        string finalsGen,
        bool ranked,
        bool twoCourts,
        bool scorer,
        List<int>? teams = null,
        List<int>? officials = null) {
        officials ??= [];
        teams ??= [];
        
        var searchableName = Utilities.ToSearchable(name);
        
        var tournament = new Tournament
        {
            Name = name,
            SearchableName = searchableName,
            FixturesType = fixturesGen,
            FinalsType = finalsGen,
            Ranked = ranked,
            TwoCourts = twoCourts,
            HasScorer = scorer,
            BadmintonServes = true,
            ImageUrl = $"https://api.squarers.club/tournaments/image?name={searchableName}"
        };

        db.Tournaments.Add(tournament);
        db.SaveChanges();
        
        foreach (var teamId in teams.OrderBy(i => i))
        {
            db.TournamentTeams.Add(new TournamentTeam
            {
                TournamentId = tournament.Id,
                TeamId = teamId
            });
        }
        
        foreach (var officialId in officials.OrderBy(i => i))
        {
            db.TournamentOfficials.Add(new TournamentOfficial
            {
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
}