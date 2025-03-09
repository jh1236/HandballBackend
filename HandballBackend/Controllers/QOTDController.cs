using System.ComponentModel.DataAnnotations;
using HandballBackend.Database.SendableTypes;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class QOTDController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QuoteOfTheDayData> GetQOTD() {
        var db = new HandballContext();
        var today = DateTime.Today.DayOfYear;
        var quotes = db.QuotesOfTheDay
            .ToArray();
        var quote = quotes[today % quotes.Length];
        return quote.ToSendableData();
    }
}