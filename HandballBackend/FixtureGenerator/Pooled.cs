using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers.GameManagement;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.FixtureGenerator;

public class Pooled : AbstractFixtureGenerator {
    private readonly int _tournamentId;


    public Pooled(int tournamentId) : base(tournamentId, true, false, true) {
        _tournamentId = tournamentId;
    }

    public override void BeginTournament() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;
        tournament.IsPooled = true;

        var teams = db.TournamentTeams
            .Where(t => t.TournamentId == _tournamentId)
            .IncludeRelevant()
            .ToArray().OrderByDescending(t => t.Team.Elo()).ToList();

        var pool = 0;
        foreach (var team in teams) {
            team.Pool = 1 + pool;
            pool = 1 - pool;
        }
    }

    public override void EndOfRound() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;
        var tournamentTeams = db.TournamentTeams
            .Where(t => t.TournamentId == _tournamentId)
            .IncludeRelevant()
            .ToList();

        var rounds = db.Games
            .Where(g => g.TournamentId == _tournamentId)
            .Select(g => g.Round).OrderByDescending(g => g).FirstOrDefault(0);

        var poolOne = tournamentTeams.Where(tt => tt.Pool == 1).Select(tt => tt.Team).ToList();
        var poolTwo = tournamentTeams.Where(tt => tt.Pool == 2).Select(tt => tt.Team).ToList();

        if (poolOne.Count % 2 != 0) {
            poolOne.Add(db.Teams.First(t => t.Id == 1));
        }

        if (poolTwo.Count % 2 != 0) {
            poolTwo.Add(db.Teams.First(t => t.Id == 1));
        }

        if (Math.Max(poolTwo.Count, poolOne.Count) <= rounds + 1) {
            // we are now in finals
            tournament.InFinals = true;
            db.SaveChanges();
            return;
        }

        for (var i = 0; i < rounds; i++) {
            poolOne.Insert(0, poolOne.Last());
            poolOne.RemoveAt(poolOne.Count - 1);
            poolTwo.Insert(0, poolTwo.Last());
            poolTwo.RemoveAt(poolTwo.Count - 1);
        }

        for (var i = 0; i < poolOne.Count / 2; i++) {
            var teamOne = poolOne[i];
            var teamTwo = poolOne[poolOne.Count - i - 1];
            GameManager.CreateGame(_tournamentId, teamOne.Id, teamTwo.Id, round: rounds + 1);
        }

        base.EndOfRound();
    }
}