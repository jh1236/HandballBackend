using HandballBackend.Database.Models;

namespace HandballBackend.Utils;

/*
 * K = 40.0
 * initial_elo = 1500
 * D = 3000.0
 *
 *
 * numbers = {0: "One", 1: "Two", 2: "Three", 3: "Four"}
 *
 *
 * def probability(other, me):
 *     return 1.0 / (1.0 + math.pow(10, K * (other - me) / D))
 *
 *
 * def calc_elo(elo, elo_other, first_won):
 *     pa = probability(elo_other, elo)
 *     delta_ra = K * (first_won - pa)
 *     return delta_ra
 *
 */

public static class EloCalculator {
    private static double K = 40.0;
    private static double D = 3000.0;

    public static double InitialElo = 1500.0;

    private static double Probability(double opponentElo, double myElo) {
        return 1.0 / (1.0 + Math.Pow(10, K * (opponentElo - myElo) / D));
    }

    public static double CalculateEloDelta(double myElo, double opponentElo, bool win) {
        var pa = Probability(opponentElo, myElo);
        var delta = K * (win ? 1 - pa : -pa);
        return delta;
    }

    public static void CalculateElos(Game game, bool isForfeit) { }
}