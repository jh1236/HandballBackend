using HandballBackend.Database;
using HandballBackend.Database.Models;
using HandballBackend.Database.SendableTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase {
    [HttpGet("{searchable}")]
    public ActionResult<TeamData> GetSingle(string searchable, [FromQuery] string? tournament = null,
        [FromQuery] bool formatData = false,
        [FromQuery] bool returnTournament = false) {
        var db = new HandballContext();
        var tourney = db.Tournaments.FirstOrDefault(t => t.SearchableName == tournament);
        var team = db.Teams
            .Where(t => t.SearchableName == searchable)
            .IncludeRelevant()
            .Include(t => t.PlayerGameStats)
            .ThenInclude(pgs => pgs.Game)
            .Select(t => t.ToSendableData(tourney, true, true)).FirstOrDefault();
        if (team is null) {
            return NotFound();
        }

        return team;
    }

    [HttpGet]
    public ActionResult<TeamData[]> GetMultiple([FromQuery] string? tournament = null,
        [FromQuery] List<string>? players = null,
        [FromQuery] bool includeStats = false,
        [FromQuery] bool includePlayerStats = false) {
        var db = new HandballContext();
        var tourney = db.Tournaments.FirstOrDefault(a => a.SearchableName == tournament);
        IQueryable<Team> teams;
        if (tourney is not null) {
            IQueryable<TournamentTeam> query = db.TournamentTeams
                .Where(t => t.TournamentId == tourney.Id)
                .Include(t => t.Team.Captain)
                .Include(t => t.Team.NonCaptain)
                .Include(t => t.Team.Substitute);
            if (includeStats) {
                query = query
                    .Include(t => t.Team.PlayerGameStats)
                    .ThenInclude(pgs => pgs.Game);
            }

            teams = query.Select(t => t.Team);
        }
        else {
            //Not null captain removes bye team
            var query = db.Teams.IncludeRelevant();

            if (includeStats) {
                query = query
                    .Include(t => t.PlayerGameStats)
                    .ThenInclude(pgs => pgs.Game);
            }

            teams = query.Where(t => t.Captain != null);
        }

        return teams.OrderBy(t => t.SearchableName)
            .Select(t => t.ToSendableData(tourney, includeStats, includePlayerStats)).ToArray();
    }
}