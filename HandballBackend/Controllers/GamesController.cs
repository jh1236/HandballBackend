using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase {
    [HttpGet("{gameNumber:int}")]
    public ActionResult<Dictionary<string, dynamic?>> GetSingleGame(
        int gameNumber,
        [FromQuery] bool includeGameEvents = false,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool formatData = false
    ) {
        var db = new HandballContext();
        var query = db.Games.IncludeRelevant();
        if (includeGameEvents) {
            query = query.Include(g => g.Events);
        }

        var game = query.FirstOrDefault(g => g.GameNumber == gameNumber);
        if (game is null) {
            return NotFound();
        }

        return Utilities.WrapInDictionary("game", game.ToSendableData(includeGameEvents, includeStats, formatData));
    }
}