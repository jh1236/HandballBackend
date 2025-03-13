using System.Text.RegularExpressions;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Utils;

public static partial class Utilities {
    public static Dictionary<string, dynamic?> WrapInDictionary(string key, dynamic? objectToWrap) {
        return new Dictionary<string, dynamic?> {{key, objectToWrap}};
    }

    public static string FixImageUrl(string? urlIn) {
        if (urlIn is null) {
            return Config.MY_ADDRESS + "/api/image?name=blank";
        }

        return urlIn.StartsWith('/') ? Config.MY_ADDRESS + urlIn : urlIn;
    }

    public static bool TournamentOrElse(HandballContext db, string? searchable, out Tournament? tournament) {
        if (searchable is null) {
            tournament = null;
            return true;
        }

        tournament = db.Tournaments.FirstOrDefault(t => t.SearchableName == searchable);
        return tournament is not null;
    }

    public static string SplitCamelCase(string input) {
        return SplitCamelCase().Replace(input, " $1").Trim();
    }

    [GeneratedRegex("([A-Z])", RegexOptions.Compiled)]
    private static partial System.Text.RegularExpressions.Regex SplitCamelCase();
}