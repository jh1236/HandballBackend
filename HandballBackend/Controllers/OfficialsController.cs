using System.ComponentModel.DataAnnotations;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class OfficialsController : ControllerBase {
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Dictionary<string, dynamic?>> GetOfficials(
        [FromQuery] string? tournament = null
    ) {
        var db = new HandballContext();
        OfficialData[] officials = null;

        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("Invalid Tournament");
        }

        if (tourney is not null) {
            officials = db.TournamentOfficials
                .Where(a => a.TournamentId == tourney.Id)
                .Select(to => to.Official.ToSendableData(tourney, false))
                .ToArray();
        }
        else {

            officials = db.Officials
                .Select(o => o.ToSendableData(null, false))
                .ToArray();
        }


        return Utilities.WrapInDictionary("officials", officials);
    }

    [HttpGet("{searchable}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Dictionary<string, dynamic?>> GetSingleOfficial(
        string searchable,
        [FromQuery] string? tournament = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false
    ) {
        var db = new HandballContext();
        var official = db.Officials
            .FirstOrDefault(o => o.Person.SearchableName == searchable);
        if (official is null) {
            return NotFound("Invalid Name");
        }

        if (Utilities.TournamentOrElse(db, tournament, out var tourney)) {
            return BadRequest("invalid Tournament");
        }

        return Utilities.WrapInDictionary("official", official.ToSendableData(tourney));
        
    }
}