using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Mvc;
//TODO: change frontend to use /api/AuthController
namespace HandballBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthController : ControllerBase {
    
    public class LoginRequest
    {
        public required int UserID { get; set; }
        public required string Password { get; set; }
        public bool LongSession { get; set; } = false;
    }
    
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<Dictionary<string, dynamic>> Login (
        [FromBody] LoginRequest loginRequest
        ) {
        
        int userID = loginRequest.UserID;
        string password = loginRequest.Password;
        bool longSession = loginRequest.LongSession;
        
        var user = PermissionHelper.Login(userID, password, longSession);
        if (user?.SessionToken is null)
        {
            return Unauthorized();
        }
        var response = new Dictionary<string, dynamic>
        {
            ["token"] = user.SessionToken,
            ["userID"] = user.Id,
            ["username"] = user.Name,
            ["permissionLevel"] = user.PermissionLevel
        };
        return response;
    }

    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        PermissionHelper.Logout();
        return NoContent();
    }
    
}