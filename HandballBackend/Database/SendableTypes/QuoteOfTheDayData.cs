using HandballBackend.Database.Models;

namespace HandballBackend.Database.SendableTypes;

public class QuoteOfTheDayData(QuoteOfTheDay qotd) {
    public string Quote { get; private set; } = qotd.Quote;
    public string Author { get; private set; } = qotd.Author;
}