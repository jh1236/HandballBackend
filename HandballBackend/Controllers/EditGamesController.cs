using HandballBackend.EndpointHelpers.GameManagement;
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

        var g = GameManager.CreateGame(tournament!.Id, create.playersOne, create.playersTwo, create.teamOne,
            create.teamTwo,
            official.Id);
        return Created(Config.MY_ADDRESS + $"/api/games/{g.GameNumber}",
            Utilities.WrapInDictionary("game", g.ToSendableData()));
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

    public class ScorePointRequest {
        public required int id { get; set; }
        public required bool firstTeam { get; set; }
        public bool? leftPlayer { get; set; }
        public string? playerSearchable { get; set; }
        public string? method { get; set; }
    }

    [HttpPost("score")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ScorePoint([FromBody] ScorePointRequest scorePointRequest) {
        if (scorePointRequest.leftPlayer.HasValue) {
            GameManager.ScorePoint(scorePointRequest.id, scorePointRequest.firstTeam,
                scorePointRequest.leftPlayer.Value, scorePointRequest.method);
        } else if (!string.IsNullOrEmpty(scorePointRequest.playerSearchable)) {
            GameManager.ScorePoint(scorePointRequest.id, scorePointRequest.firstTeam,
                scorePointRequest.playerSearchable, scorePointRequest.method);
        } else {
            return BadRequest("Either leftPlayer or playerSearchable must be provided.");
        }

        return NoContent();
    }

    public class CardRequest {
        public required int id { get; set; }
        public required bool firstTeam { get; set; }
        public bool? leftPlayer { get; set; }
        public string? playerSearchable { get; set; }
        public string color { get; set; }
        public string? reason { get; set; }
        public int duration { get; set; }
    }

    [HttpPost("card")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Card([FromBody] CardRequest cardRequest) {
        if (cardRequest.leftPlayer.HasValue) {
            GameManager.Card(cardRequest.id, cardRequest.firstTeam, cardRequest.leftPlayer.Value, cardRequest.color,
                cardRequest.duration, cardRequest.reason ?? "Not Provided");
        } else if (!string.IsNullOrEmpty(cardRequest.playerSearchable)) {
            GameManager.Card(cardRequest.id, cardRequest.firstTeam, cardRequest.playerSearchable, cardRequest.color,
                cardRequest.duration, cardRequest.reason ?? "Not Provided");
        } else {
            return BadRequest("Either leftPlayer or playerSearchable must be provided.");
        }

        return NoContent();
    }

    public class AceRequest {
        public required int id { get; set; }
    }

    [HttpPost("ace")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Ace([FromBody] AceRequest aceRequest) {
        GameManager.Ace(aceRequest.id);
        return NoContent();
    }

    public class FaultRequest {
        public required int id { get; set; }
    }

    [HttpPost("fault")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Fault([FromBody] FaultRequest faultRequest) {
        GameManager.Fault(faultRequest.id);
        return NoContent();
    }

    public class TimeoutRequest {
        public required int id { get; set; }
        public required bool firstTeam { get; set; }
    }

    [HttpPost("timeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Timeout([FromBody] TimeoutRequest timeoutRequest) {
        GameManager.Timeout(timeoutRequest.id, timeoutRequest.firstTeam);
        return NoContent();
    }

    public class ForfeitRequest {
        public required int id { get; set; }
        public required bool firstTeam { get; set; }
    }

    [HttpPost("forfeit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Forfeit([FromBody] ForfeitRequest forfeitRequest) {
        GameManager.Forfeit(forfeitRequest.id, forfeitRequest.firstTeam);
        return NoContent();
    }

    public class EndTimeoutRequest {
        public required int id { get; set; }
    }

    [HttpPost("endTimeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult EndTimeout([FromBody] EndTimeoutRequest endTimeoutRequest) {
        GameManager.EndTimeout(endTimeoutRequest.id);
        return NoContent();
    }

    public class SubstituteRequest {
        public required int id { get; set; }
        public required bool firstTeam { get; set; }
        public required string playerSearchable { get; set; }
    }

    [HttpPost("substitute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Substitute([FromBody] SubstituteRequest substituteRequest) {
        GameManager.Substitute(substituteRequest.id, substituteRequest.firstTeam, substituteRequest.playerSearchable);
        return NoContent();
    }

    public class UndoRequest {
        public required int id { get; set; }
    }

    [HttpPost("undo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Undo([FromBody] UndoRequest undoRequest) {
        GameManager.Undo(undoRequest.id);
        return NoContent();
    }

    public class EndGameRequest {
        public int GameNumber { get; set; }
        public string? BestPlayerSearchable { get; set; }
        public int TeamOneRating { get; set; }
        public int TeamTwoRating { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? ProtestReasonTeamOne { get; set; }
        public string? ProtestReasonTeamTwo { get; set; }
        public string NotesTeamOne { get; set; } = string.Empty;
        public string NotesTeamTwo { get; set; } = string.Empty;
        public bool MarkedForReview { get; set; }
    }

    [HttpPost("end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult EndGame([FromBody] EndGameRequest request) {
        GameManager.End(
            request.GameNumber,
            request.BestPlayerSearchable,
            request.TeamOneRating,
            request.TeamTwoRating,
            request.Notes,
            request.ProtestReasonTeamOne,
            request.ProtestReasonTeamTwo,
            request.NotesTeamOne,
            request.NotesTeamTwo,
            request.MarkedForReview
        );

        return NoContent();
    }
}