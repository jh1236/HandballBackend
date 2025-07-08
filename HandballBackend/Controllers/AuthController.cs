using System.Security.Claims;
using HandballBackend.Authentication;
using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

//TODO: change frontend to use /api/AuthController
namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthController(IAuthorizationService authorizationService) : ControllerBase {
    public class LoginRequest {
        public required int UserID { get; set; }
        public required string Password { get; set; }
        public bool LongSession { get; set; } = false;
    }

    public class LoginResponse {
        public required string Token { get; set; }
        public int UserID { get; set; }
        public long Timeout { get; set; }
        public required string Username { get; set; }
        public int PermissionLevel { get; set; }
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest loginRequest) {
        var userId = loginRequest.UserID;
        var password = loginRequest.Password;
        var longSession = loginRequest.LongSession;

        var user = PermissionHelper.Login(userId, password, longSession);
        if (user?.SessionToken is null) {
            return Unauthorized();
        }

        var response = new LoginResponse {
            Token = user.SessionToken,
            UserID = user.Id,
            Timeout = user.TokenTimeout!.Value * 1000L, //convert to ms for frontend
            Username = user.Name,
            PermissionLevel = user.PermissionLevel,
        };
        return response;
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    [HttpPost("logout")]
    public Task<IActionResult> Logout() {
        var userId = int.Parse(
            HttpContext.User.Claims.Single(c => c.Type == CustomClaimTypes.Token).Value
        );
        PermissionHelper.Logout(userId);
        return Task.FromResult<IActionResult>(NoContent());
    }
}