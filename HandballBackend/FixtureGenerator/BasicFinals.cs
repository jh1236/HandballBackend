﻿using HandballBackend.EndpointHelpers;
using HandballBackend.EndpointHelpers.GameManagement;

namespace HandballBackend.FixtureGenerator;

public class BasicFinals : AbstractFixtureGenerator {
    private readonly int _tournamentId;


    public BasicFinals(int tournamentId) : base(tournamentId, true, true) {
        _tournamentId = tournamentId;
    }

    public override bool EndOfRound() {
        var db = new HandballContext();
        var tournament = db.Tournaments.Find(_tournamentId)!;

        var finalsGames = db.Games.Where(g => g.TournamentId == _tournamentId && g.IsFinal).OrderBy(g => g.Id).ToList();

        if (finalsGames.Count > 2) {
            // each round is 2 games, so > 2 means we've had both rounds
            EndTournament();
            return true;
        }

        if (finalsGames.Count != 0) {
            GameManager.CreateGame(_tournamentId, finalsGames[0].LosingTeamId, finalsGames[1].LosingTeamId,
                isFinal: true, round: finalsGames[0].Round + 1);
            GameManager.CreateGame(_tournamentId, finalsGames[0].WinningTeamId!.Value,
                finalsGames[1].WinningTeamId!.Value, isFinal: true, round: finalsGames[0].Round + 1);
        } else {
            var (ladder, _, _) = LadderHelper.GetTournamentLadder(db, tournament);
            var lastGame = db.Games.Where(g => g.TournamentId == _tournamentId).OrderByDescending(g => g.Id).First();
            GameManager.CreateGame(_tournamentId, ladder![0].Id, ladder[3].Id, isFinal: true,
                round: lastGame.Round + 1);
            GameManager.CreateGame(_tournamentId, ladder[1].Id, ladder[2].Id, isFinal: true,
                round: lastGame.Round + 1);
        }

        return base.EndOfRound();
    }
}