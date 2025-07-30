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

[Authorize]
[ApiController]
[Route("api/games/update")]
public class EditGamesController : ControllerBase {
    public class CreateRequest {
        public required string Tournament { get; init; }
        public string? TeamOne { get; set; } = null;
        public string? TeamTwo { get; set; } = null;
        public string[]? PlayersOne { get; set; } = null;
        public string[]? PlayersTwo { get; set; } = null;
        public required string Official { get; set; }
        public string? Scorer { get; set; } = null;

        public bool BlitzGame { get; set; } = false;
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

        if (!PermissionHelper.IsUmpire(tournament)) {
            return Forbid();
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
            create.BlitzGame,
            official.Id,
            scorer?.Id ?? -1
        );

        return Created(Config.MY_ADDRESS + $"/api/games/{g.GameNumber}", new CreateResponse {
            Game = g.ToSendableData()
        });
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
    public IActionResult StartGame(
        [FromBody] StartRequest startRequest
    ) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == startRequest.Id))) {
            return Forbid();
        }

        GameManager.StartGame(startRequest.Id, startRequest.SwapService, startRequest.TeamOne, startRequest.TeamTwo,
            startRequest.TeamOneIga, startRequest.Official, startRequest.Scorer);
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
    public IActionResult ScorePointForGame([FromBody] ScorePointRequest scorePointRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == scorePointRequest.Id))) {
            return Forbid();
        }

        if (!string.IsNullOrEmpty(scorePointRequest.PlayerSearchable)) {
            GameManager.ScorePoint(scorePointRequest.Id, scorePointRequest.FirstTeam,
                scorePointRequest.PlayerSearchable, scorePointRequest.Method);
        } else if (scorePointRequest.LeftPlayer.HasValue) {
            GameManager.ScorePoint(scorePointRequest.Id, scorePointRequest.FirstTeam,
                scorePointRequest.LeftPlayer.Value, scorePointRequest.Method);
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
    public IActionResult CardForGame([FromBody] CardRequest cardRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == cardRequest.Id))) {
            return Forbid();
        }

        if (!string.IsNullOrEmpty(cardRequest.PlayerSearchable)) {
            GameManager.Card(cardRequest.Id, cardRequest.FirstTeam, cardRequest.PlayerSearchable, cardRequest.Color,
                cardRequest.Duration, cardRequest.Reason ?? "Not Provided");
        } else if (cardRequest.LeftPlayer.HasValue) {
            GameManager.Card(cardRequest.Id, cardRequest.FirstTeam, cardRequest.LeftPlayer.Value, cardRequest.Color,
                cardRequest.Duration, cardRequest.Reason ?? "Not Provided");
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
    public IActionResult AceForGame([FromBody] AceRequest aceRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == aceRequest.Id))) {
            return Forbid();
        }

        GameManager.Ace(aceRequest.Id);
        return NoContent();
    }

    public class FaultRequest {
        public required int Id { get; set; }
    }

    [HttpPost("fault")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult FaultForGame([FromBody] FaultRequest faultRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == faultRequest.Id))) {
            return Forbid();
        }

        GameManager.Fault(faultRequest.Id);
        return NoContent();
    }

    public class TimeoutRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
    }

    [HttpPost("timeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult TimeoutForGame([FromBody] TimeoutRequest timeoutRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == timeoutRequest.Id))) {
            return Forbid();
        }

        GameManager.Timeout(timeoutRequest.Id, timeoutRequest.FirstTeam);
        return NoContent();
    }

    public class ForfeitRequest {
        public required int Id { get; set; }
        public required bool FirstTeam { get; set; }
    }

    [HttpPost("forfeit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ForfeitGame([FromBody] ForfeitRequest forfeitRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == forfeitRequest.Id))) {
            return Forbid();
        }

        GameManager.Forfeit(forfeitRequest.Id, forfeitRequest.FirstTeam);
        return NoContent();
    }

    public class AbandonRequest {
        public required int Id { get; set; }
    }

    [HttpPost("abandon")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult AbandonGame([FromBody] AbandonRequest forfeitRequest) {
        GameManager.Abandon(forfeitRequest.Id);
        return NoContent();
    }

    public class EndTimeoutRequest {
        public required int Id { get; set; }
    }

    [HttpPost("endTimeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult EndTimeoutForGame([FromBody] EndTimeoutRequest endTimeoutRequest) {
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
    public IActionResult SubstituteForGame([FromBody] SubstituteRequest substituteRequest) {
        if (!string.IsNullOrEmpty(substituteRequest.PlayerSearchable)) {
            GameManager.Substitute(substituteRequest.Id, substituteRequest.FirstTeam,
                substituteRequest.PlayerSearchable);
        } else if (substituteRequest.LeftPlayer.HasValue) {
            GameManager.Substitute(substituteRequest.Id, substituteRequest.FirstTeam,
                substituteRequest.LeftPlayer.Value);
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
    public IActionResult UndoForGame([FromBody] UndoRequest undoRequest) {
        GameManager.Undo(undoRequest.Id);
        return NoContent();
    }

    public class DeleteRequest {
        public required int Id { get; set; }
    }

    [HttpPost("delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DeleteGame([FromBody] DeleteRequest deleteRequest) {
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
    public IActionResult EndGame([FromBody] EndGameRequest endGameRequest) {
        if (!PermissionHelper.IsUmpire(new HandballContext().Games.First(g => g.GameNumber == endGameRequest.Id))) {
            return Forbid();
        }

        GameManager.End(
            endGameRequest.Id,
            endGameRequest.Votes,
            endGameRequest.TeamOneRating,
            endGameRequest.TeamTwoRating,
            endGameRequest.Notes,
            endGameRequest.ProtestReasonTeamOne,
            endGameRequest.ProtestReasonTeamTwo,
            endGameRequest.NotesTeamOne,
            endGameRequest.NotesTeamTwo,
            endGameRequest.MarkedForReview
        );

        return NoContent();
    }

    public class AlertRequest {
        public required int Id { get; set; }
    }

    [HttpPost("alert")]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult AlertGame([FromBody] AlertRequest alertRequest) {
        if (!PermissionHelper.IsUmpireManager(
                new HandballContext().Games.First(g => g.GameNumber == alertRequest.Id))) {
            return Forbid();
        }

        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().First(g => alertRequest.Id == g.GameNumber);
        _ = TextHelper.TextPeopleForGame(game);
        return NoContent();
    }

    public class ResolveRequest {
        public required int Id { get; set; }
    }

    [HttpPost("resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [TournamentAuthorize(PermissionType.UmpireManager)]
    public IActionResult ResolveGame([FromBody] ResolveRequest resolveRequest) {
        if (!PermissionHelper.IsUmpireManager(
                new HandballContext().Games.First(g => g.GameNumber == resolveRequest.Id))) {
            return Forbid();
        }
        GameManager.Resolve(resolveRequest.Id);
        return NoContent();
    }
}