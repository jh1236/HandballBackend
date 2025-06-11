using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoreboardController : ControllerBase {
    public Dictionary<int, List<WebSocket>> Sockets = new();



    public async Task<IActionResult> GetScoreboard(int gameId) {
        if (!HttpContext.WebSockets.IsWebSocketRequest) {
            return BadRequest();
        }

        using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
        return Ok();
    }
}