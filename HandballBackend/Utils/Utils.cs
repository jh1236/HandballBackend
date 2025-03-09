using HandballBackend.Database.Models;

namespace HandballBackend.Utils;

public static class Utilities {
    public static Dictionary<string, dynamic?> WrapInDictionary(string key, dynamic? objectToWrap) {
        return new Dictionary<string, dynamic?> {{key, objectToWrap}};
    }

    public static bool TournamentOrElse(HandballContext db, string? searchable, out Tournament? tournament) {
        if (searchable is null) {
            tournament = null;
            return true;
        }
        tournament = db.Tournaments.FirstOrDefault(t => t.SearchableName == searchable);
        return tournament is null;
    }
}