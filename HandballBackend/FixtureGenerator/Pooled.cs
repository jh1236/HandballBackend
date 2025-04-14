using HandballBackend.Database;
using HandballBackend.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.FixtureGenerator;

public class Pooled : AbstractFixtureGenerator {
    private readonly int _tournamentId;


    public Pooled(int tournamentId) : base(tournamentId, true, false, true) {
        _tournamentId = tournamentId;
    }

    public override void EndOfRound() {
        var db = new HandballContext();
        var teams = db.TournamentTeams
            .Where(t => t.TournamentId == _tournamentId)
            .Select(t => t.Team)
            .IncludeRelevant()
            .ToList();

        var games = db.Games
            .Where(g => g.TournamentId == _tournamentId)
            .OrderByDescending(g => g.Round);


        base.EndOfRound();
    }

    public override void BeginTournament() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;
        tournament.IsPooled = true;
        
        var teams = db.TournamentTeams
            .Where(t => t.TournamentId == _tournamentId)
            .Select(t => t.Team)
            .IncludeRelevant()
            .ToList();
        var pool = 0;
        
    }
}