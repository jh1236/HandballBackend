using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/games/update")]
public class EditGamesController : ControllerBase {
    public class CreateRequest {
        public required string tournament { get; set; }
        public string? teamOne { get; set; } = null;
        public string? teamTwo { get; set; } = null;
        public string[]? playersOne { get; set; } = null;
        public string[]? playersTwo { get; set; } = null;
        public required string official { get; set; }
        public string? scorer { get; set; } = null;
    }

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateGame([FromBody] CreateRequest create) {
        var db = new HandballContext();
        if (!Utilities.TournamentOrElse(db, create.tournament, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        var official = db.Officials.FirstOrDefault(o => o.Person.SearchableName == create.official);
        if (official == null) {
            return BadRequest("Official not found");
        }

        var g = GameManager.CreateGame(tournament!.Id, create.playersOne, create.playersTwo, create.teamOne, create.teamTwo,
            official.Id);
        return Created(Config.MY_ADDRESS + $"/api/games/{g.GameNumber}", Utilities.WrapInDictionary("game", g.ToSendableData()));
    }

    public class StartRequest {
        public required int id { get; set; }
        public required bool swapService { get; set; }
        public required string[] teamOne { get; set; }
        public required string[] teamTwo { get; set; }
        public required bool teamOneIGA { get; set; }

        public string? official { get; set; } = null;
        public string? scorer { get; set; } = null;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Start(
        [FromBody] StartRequest startRequest
    ) {
        GameManager.StartGame(startRequest.id, startRequest.swapService, startRequest.teamOne, startRequest.teamTwo,
            startRequest.teamOneIGA, startRequest.official, startRequest.scorer);
        return NoContent();
    }
}