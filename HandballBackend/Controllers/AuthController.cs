using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Mvc;

//TODO: change frontend to use /api/AuthController
namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthController : ControllerBase {
    public class LoginRequest {
        public required int UserID { get; set; }
        public required string Password { get; set; }
        public bool LongSession { get; set; } = false;
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, dynamic>> Login(
        [FromBody] LoginRequest loginRequest
    ) {
        var userId = loginRequest.UserID;
        var password = loginRequest.Password;
        var longSession = loginRequest.LongSession;

        var user = PermissionHelper.Login(userId, password, longSession);
        if (user?.SessionToken is null) {
            return Unauthorized();
        }

        var response = new Dictionary<string, dynamic> {
            ["token"] = user.SessionToken,
            ["userID"] = user.Id,
            ["timeout"] = user.TokenTimeout!,
            ["username"] = user.Name,
            ["permissionLevel"] = user.PermissionLevel
        };
        return response;
    }

    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Logout() {
        if (!PermissionHelper.HasPermission(PermissionType.LoggedIn)) {
            return BadRequest("You must be logged in to log out.");
        }

        PermissionHelper.Logout();
        return NoContent();
    }
}