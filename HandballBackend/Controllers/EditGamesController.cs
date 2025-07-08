using HandballBackend.Authentication;
using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.EndpointHelpers.GameManagement;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Twilio.Jwt.Taskrouter;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/games/update")]
[Authorize(Roles = nameof(PermissionType.Umpire))]
public class EditGamesController : ControllerBase {
    public class CreateRequest {
        public required string Tournament { get; init; }
        public string? TeamOne { get; set; } = null;
        public string? TeamTwo { get; set; } = null;
        public string[]? PlayersOne { get; set; } = null;
        public string[]? PlayersTwo { get; set; } = null;
        public required string Official { get; set; }
        public string? Scorer { get; set; } = null;
    }

    public class CreateResponse {
        public required GameData Game { get; set; }
    }

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateGame([FromBody] CreateRequest create) {
        var db = new HandballContext();
        if (!Utilities.TournamentOrElse(db, create.Tournament, out var tournament)) {
            return BadRequest("Invalid tournament");
        }

        var official = db.Officials.FirstOrDefault(o => o.Person.SearchableName == create.Official);
        var scorer = db.Officials.FirstOrDefault(o => o.Person.SearchableName == create.Scorer);
        if (official == null) {
            return BadRequest("Official not found");
        }

        var g = GameManager.CreateGame(
            tournament!.Id,
            create.PlayersOne,
            create.PlayersTwo,
            create.TeamOne,
            create.TeamTwo,
            official.Id,
            scorer?.Id ?? -1
        );

        return Created(
            Config.MY_ADDRESS + $"/api/games/{g.GameNumber}",
            new CreateResponse { Game = g.ToSendableData() }
        );
    }

    public class StartRequest {
        public required int Id { get; set; }
        public required bool SwapService { get; set; }
        public required string[] TeamOne { get; set; }
        public required string[] TeamTwo { get; set; }
        public required bool TeamOneIga { get; set; }

        public string? Official { get; set; } = null;
        public string? Scorer { get; set; } = null;
    }

    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Start([FromBody] StartRequest startRequest) {
        GameManager.StartGame(
            startRequest.Id,
            startRequest.SwapService,
            startRequest.TeamOne,
            startRequest.TeamTwo,
            startRequest.TeamOneIga,
            startRequest.Official,
            startRequest.Scorer
        );
        return NoContent();
    }

    public class ScorePointRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
        public bool? LeftPlayer { get; set; }
        public string? PlayerSearchable { get; set; }
        public string? Method { get; set; }
    }

    [HttpPost("score")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ScorePoint([FromBody] ScorePointRequest scorePointRequest) {
        if (!string.IsNullOrEmpty(scorePointRequest.PlayerSearchable)) {
            GameManager.ScorePoint(
                scorePointRequest.Id,
                scorePointRequest.FirstTeam,
                scorePointRequest.PlayerSearchable,
                scorePointRequest.Method
            );
        } else if (scorePointRequest.LeftPlayer.HasValue) {
            GameManager.ScorePoint(
                scorePointRequest.Id,
                scorePointRequest.FirstTeam,
                scorePointRequest.LeftPlayer.Value,
                scorePointRequest.Method
            );
        } else {
            return BadRequest("Either leftPlayer or playerSearchable must be provided.");
        }
        return NoContent();
    }

    public class CardRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
        public bool? LeftPlayer { get; set; }
        public string? PlayerSearchable { get; set; }
        public required string Color { get; set; }
        public string? Reason { get; set; }
        public int Duration { get; set; }
    }

    [HttpPost("card")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Card([FromBody] CardRequest cardRequest) {
        if (!string.IsNullOrEmpty(cardRequest.PlayerSearchable)) {
            GameManager.Card(
                cardRequest.Id,
                cardRequest.FirstTeam,
                cardRequest.PlayerSearchable,
                cardRequest.Color,
                cardRequest.Duration,
                cardRequest.Reason ?? "Not Provided"
            );
        } else if (cardRequest.LeftPlayer.HasValue) {
            GameManager.Card(
                cardRequest.Id,
                cardRequest.FirstTeam,
                cardRequest.LeftPlayer.Value,
                cardRequest.Color,
                cardRequest.Duration,
                cardRequest.Reason ?? "Not Provided"
            );
        } else {
            return BadRequest("Either leftPlayer or playerSearchable must be provided.");
        }
        return NoContent();
    }

    public class AceRequest {
        public required int Id { get; set; }
    }

    [HttpPost("ace")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Ace([FromBody] AceRequest aceRequest) {
        GameManager.Ace(aceRequest.Id);
        return NoContent();
    }

    public class FaultRequest {
        public required int Id { get; set; }
    }

    [HttpPost("fault")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Fault([FromBody] FaultRequest faultRequest) {
        GameManager.Fault(faultRequest.Id);
        return NoContent();
    }

    public class TimeoutRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
    }

    [HttpPost("timeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Timeout([FromBody] TimeoutRequest timeoutRequest) {
        GameManager.Timeout(timeoutRequest.Id, timeoutRequest.FirstTeam);
        return NoContent();
    }

    public class ForfeitRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
    }

    [HttpPost("forfeit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Forfeit([FromBody] ForfeitRequest forfeitRequest) {
        GameManager.Forfeit(forfeitRequest.Id, forfeitRequest.FirstTeam);
        return NoContent();
    }

    public class EndTimeoutRequest {
        public required int Id { get; set; }
    }

    [HttpPost("endTimeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult EndTimeout([FromBody] EndTimeoutRequest endTimeoutRequest) {
        GameManager.EndTimeout(endTimeoutRequest.Id);
        return NoContent();
    }

    public class SubstituteRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
        public string? PlayerSearchable { get; set; }
        public bool? LeftPlayer { get; set; }
    }

    [HttpPost("substitute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Substitute([FromBody] SubstituteRequest substituteRequest) {
        if (!string.IsNullOrEmpty(substituteRequest.PlayerSearchable)) {
            GameManager.Substitute(
                substituteRequest.Id,
                substituteRequest.FirstTeam,
                substituteRequest.PlayerSearchable
            );
        } else if (substituteRequest.LeftPlayer.HasValue) {
            GameManager.Substitute(
                substituteRequest.Id,
                substituteRequest.FirstTeam,
                substituteRequest.LeftPlayer.Value
            );
        } else {
            return BadRequest("Either leftPlayer or playerSearchable must be provided.");
        }

        return NoContent();
    }

    public class UndoRequest {
        public required int Id { get; set; }
    }

    [HttpPost("undo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Undo([FromBody] UndoRequest undoRequest) {
        GameManager.Undo(undoRequest.Id);
        return NoContent();
    }

    public class DeleteRequest {
        public required int Id { get; set; }
    }

    [HttpPost("delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Delete([FromBody] DeleteRequest deleteRequest) {
        GameManager.Delete(deleteRequest.Id);
        return NoContent();
    }

    public class EndGameRequest {
        public int Id { get; set; }
        public required List<string> Votes { get; set; }
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
            request.Id,
            request.Votes,
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

    public class AlertRequest {
        public required int Id { get; set; }
    }

    [HttpPost("alert")]
    [Authorize(Policy = Policies.IsUmpireManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Alert([FromBody] AlertRequest alertRequest) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().First(g => alertRequest.Id == g.GameNumber);
        TextHelper.TextPeopleForGame(game);
        return NoContent();
    }

    public class ResolveRequest {
        public required int Id { get; set; }
    }

    [HttpPost("resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = Policies.IsUmpireManager)]
    public IActionResult Resolve([FromBody] ResolveRequest resolveRequest) {
        GameManager.Resolve(resolveRequest.Id);
        return NoContent();
    }
}