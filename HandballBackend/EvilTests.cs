using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;

namespace HandballBackend;

internal static class EvilTests {
    public static void EvilTest(int number) {
        var db = new HandballContext();
        IQueryable<Team> query = db.Teams;
        query = Team.GetRelevant(query);
        query = query.IncludeRelevant();
        Console.WriteLine(query.ToArray());
    }

    public static void MalevolentTest() {
        bool[] l = [
            false, false, true, true
        ];

        Console.WriteLine(l.OrderBy(a => a).ToList()[0]);
    }
}