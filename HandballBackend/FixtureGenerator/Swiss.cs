using HandballBackend.Database;
using HandballBackend.EndpointHelpers.GameManagement;

namespace HandballBackend.FixtureGenerator;

//TODO: Fix this so it works
public class Swiss : AbstractFixtureGenerator {
    private readonly int _tournamentId;


    public Swiss(int tournamentId) : base(tournamentId, true, true) {
        _tournamentId = tournamentId;
    }


    public override bool EndOfRound() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;
        var teams = db.TournamentTeams.Where(t => t.TournamentId == _tournamentId).Select(tt => tt.Team).ToList();
        return base.EndOfRound();
    }
}