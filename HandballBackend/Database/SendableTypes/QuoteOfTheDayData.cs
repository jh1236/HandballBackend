// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 


using HandballBackend.Database.Models;

namespace HandballBackend.Database.SendableTypes;

public class QuoteOfTheDayData(QuoteOfTheDay qotd) {
    public string quote { get; private set; } = qotd.Quote;
    public string author { get; private set; } = qotd.Author;
}