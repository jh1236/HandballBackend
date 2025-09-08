using HandballBackend.Authentication;
using HandballBackend.EndpointHelpers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//TODO: change frontend to use /api/AuthController
namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthController : ControllerBase {
    public class LoginRequest {
        // ReSharper disable once InconsistentNaming
        public required int UserID { get; [UsedImplicitly] set; }
        public required string Password { get; [UsedImplicitly] set; }
        public bool LongSession { get; [UsedImplicitly] set; } = false;
    }

    public class LoginResponse {
        public required string Token { [UsedImplicitly] get; set; }

        // ReSharper disable once InconsistentNaming
        public int UserID { [UsedImplicitly] get; set; }
        public long Timeout { [UsedImplicitly] get; set; }

        public int BasePermission { [UsedImplicitly] get; set; }

        public required string Username { [UsedImplicitly] get; set; }

        [UsedImplicitly]
        public Dictionary<string, int> Permissions { get; set; } = new();
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<LoginResponse> Login(
        [FromBody] LoginRequest loginRequest
    ) {
        var userId = loginRequest.UserID;
        var password = loginRequest.Password;
        var longSession = loginRequest.LongSession;
        var db = new HandballContext();
        var tournaments = db.Tournaments.ToList();
        var user = PermissionHelper.Login(userId, password, longSession);
        if (user?.SessionToken is null) {
            return Unauthorized();
        }

        var tournamentOfficials = user.Official?.TournamentOfficials;

        var response = new LoginResponse {
            Token = user.SessionToken,
            UserID = user.Id,
            Timeout = user.TokenTimeout!.Value * 1000L, //convert to ms for frontend
            Username = user.Name,
            BasePermission = user.PermissionLevel.ToInt(),
        };
        foreach (var tournament in tournaments) {
            response.Permissions[tournament.SearchableName] = (user.PermissionLevel == PermissionType.Admin
                ? PermissionType.Admin
                : tournament.Editable
                    ? PermissionType.Umpire
                    : PermissionType.None).ToInt();
        }

        if (user.PermissionLevel.ToInt() < PermissionType.Admin.ToInt()) {
            foreach (var tournamentOfficial in tournamentOfficials ?? []) {
                var responsePermission = tournamentOfficial.Role.ToInt();
                Console.WriteLine(responsePermission);
                response.Permissions[tournamentOfficial.Tournament.SearchableName] =
                    responsePermission;
            }
        }

        return response;
    }


    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    [HttpPost("logout")]
    public Task<IActionResult> Logout() {
        var userId = int.Parse(HttpContext.User.Claims.Single(c => c.Type == CustomClaimTypes.Token).Value);
        PermissionHelper.Logout(userId);
        return Task.FromResult<IActionResult>(NoContent());
    }
}