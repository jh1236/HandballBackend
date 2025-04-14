namespace HandballBackend.FixtureGenerator;

public class OneRound(int tournamentId) : AbstractFixtureGenerator(tournamentId, false, true, false) {
    public override void BeginTournament() {
        return;
    }
}