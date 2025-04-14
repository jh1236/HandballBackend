using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.EndpointHelpers.GameManagement;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.FixtureGenerator;

public class PooledFinals : AbstractFixtureGenerator {
    private readonly int _tournamentId;


    public PooledFinals(int tournamentId) : base(tournamentId, true, false, true) {
        _tournamentId = tournamentId;
    }

    public override void EndOfRound() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;

        var finalsGames = db.Games.Where(g => g.TournamentId == _tournamentId && g.IsFinal).OrderBy(g => g.Id).ToList();

        if (finalsGames.Count >= 2) {
            EndTournament();
        } else if (finalsGames.Count != 0) {
            GameManager.CreateGame(_tournamentId, finalsGames[0].LosingTeamId, finalsGames[1].LosingTeamId,
                isFinal: true, round: finalsGames[0].Round);
            GameManager.CreateGame(_tournamentId, finalsGames[0].WinningTeamId!.Value,
                finalsGames[1].WinningTeamId!.Value, isFinal: true, round: finalsGames[0].Round);
        } else {
            var (_, poolOne, poolTwo) = LadderHelper.GetTournamentLadder(db, tournament);
            GameManager.CreateGame(_tournamentId, poolOne![0].id, poolTwo![1].id, isFinal: true,
                round: finalsGames[0].Round);
            GameManager.CreateGame(_tournamentId, poolTwo[0].id, poolOne[1].id, isFinal: true,
                round: finalsGames[0].Round);
        }

        base.EndOfRound();
    }
}