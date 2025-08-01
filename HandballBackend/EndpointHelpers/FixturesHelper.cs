using HandballBackend.Database.SendableTypes;

namespace HandballBackend.EndpointHelpers;

public static class FixturesHelper {
    public static List<GameData> SortFixtures(List<GameData> games) {
        var courtOneGames = games.Where(g => g.Court == 0).ToArray();
        var courtTwoGames = games.Where(g => g.Court == 1).ToArray();
        var byes = games.Where(g => g.IsBye);
        var output = new List<GameData>();
        for (var i = 0; i < Math.Max(courtOneGames.Length, courtTwoGames.Length); i++) {
            if (i < courtOneGames.Length) {
                output.Add(courtOneGames[i]);
            }

            if (i < courtTwoGames.Length) {
                output.Add(courtTwoGames[i]);
            }
        }

        output.AddRange(byes);
        return output;
    }
}