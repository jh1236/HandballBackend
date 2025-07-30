using HandballBackend.Database.SendableTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class QOTDController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<QuoteOfTheDayData>> GetQotd() {
        var db = new HandballContext();
        var today = DateTime.Today.DayOfYear;
        var quotes = await db.QuotesOfTheDay
            .ToArrayAsync();
        var quote = quotes[today % quotes.Length];
        return quote.ToSendableData();
    }
}