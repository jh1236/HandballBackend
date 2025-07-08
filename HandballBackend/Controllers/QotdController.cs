using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class QotdController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QuoteOfTheDayData> GetQuoteOfTheDay() {
        HandballContext db = new();
        int today = DateTime.Today.DayOfYear;
        QuoteOfTheDay[] quotes = db.QuotesOfTheDay.ToArray();
        QuoteOfTheDay quote = quotes[today % quotes.Length];
        return quote.ToSendableData();
    }
}
