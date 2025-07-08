using HandballBackend.Database;
using HandballBackend.EndpointHelpers.GameManagement;

namespace HandballBackend.FixtureGenerator;

public class RoundRobin : AbstractFixtureGenerator {
    private readonly int _tournamentId;

    public RoundRobin(int tournamentId)
        : base(tournamentId, true, true) {
        _tournamentId = tournamentId;
    }

    public override bool EndOfRound() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;
        var tournamentTeams = db
            .TournamentTeams.Where(t => t.TournamentId == _tournamentId)
            .IncludeRelevant()
            .ToList();

        var rounds = db
            .Games.Where(g => g.TournamentId == _tournamentId)
            .OrderByDescending(g => g.Round)
            .Select(g => g.Round)
            .FirstOrDefault();

        var teams = db
            .TournamentTeams.Where(t => t.TournamentId == _tournamentId)
            .IncludeRelevant()
            .Select(t => t.Team)
            .ToList();
        if (teams.Count <= rounds + 1) {
            // we are now in finals
            tournament.InFinals = true;
            db.SaveChanges();
            return true;
        }

        for (var i = 0; i < rounds; i++) {
            teams.Insert(1, teams.Last());
            teams.RemoveAt(teams.Count - 1);
        }

        for (var i = 0; i < teams.Count / 2; i++) {
            var teamOne = teams[i];
            var teamTwo = teams[teams.Count - i - 1];
            GameManager.CreateGame(_tournamentId, teamOne.Id, teamTwo.Id, round: rounds + 1);
        }

        db.SaveChanges();
        return base.EndOfRound();
    }
}