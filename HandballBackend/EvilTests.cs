using HandballBackend.Database;

namespace HandballBackend;

static class EvilTests {
    public static void EvilTest(int number) {
        var db = new HandballContext();
        var game = db.Games.Where(v => v.GameNumber == number)
            .Take(1)
            .IncludeRelevant()
            .FirstOrDefault();
        Console.WriteLine(game);
    }
}