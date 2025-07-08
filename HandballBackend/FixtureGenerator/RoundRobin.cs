using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers.GameManagement;

namespace HandballBackend.FixtureGenerator;

public class RoundRobin : AbstractFixtureGenerator {
    private readonly int _tournamentId;

    public RoundRobin(int tournamentId)
        : base(tournamentId, true, true) =>
        _tournamentId = tournamentId;

    public override bool EndOfRound() {
        HandballContext db = new();
        Tournament tournament = db.Tournaments.Find(_tournamentId)!;
        List<TournamentTeam> tournamentTeams = db
            .TournamentTeams.Where(t => t.TournamentId == _tournamentId)
            .IncludeRelevant()
            .ToList();

        int rounds = db
            .Games.Where(g => g.TournamentId == _tournamentId)
            .OrderByDescending(g => g.Round)
            .Select(g => g.Round)
            .FirstOrDefault();

        List<Team> teams = db
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

        for (int i = 0; i < rounds; i++) {
            teams.Insert(1, teams.Last());
            teams.RemoveAt(teams.Count - 1);
        }

        for (int i = 0; i < teams.Count / 2; i++) {
            Team teamOne = teams[i];
            Team teamTwo = teams[teams.Count - i - 1];
            GameManager.CreateGame(_tournamentId, teamOne.Id, teamTwo.Id, round: rounds + 1);
        }

        db.SaveChanges();
        return base.EndOfRound();
    }
}
