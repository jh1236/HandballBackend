using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.Models;

[Table("teams")]
public class Team : IHasRelevant<Team> {
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name", TypeName = "TEXT")]
    public required string Name { get; set; }

    [Column("long_name", TypeName = "TEXT")]
    public string? LongName { get; set; }

    [Required]
    [Column("searchable_name", TypeName = "TEXT")]
    public required string SearchableName { get; set; }

    [Column("image_url", TypeName = "TEXT")]
    public string? ImageUrl { get; set; }

    [Column("captain_id")]
    public int? CaptainId { get; set; }


    [Column("non_captain_id")]
    public int? NonCaptainId { get; set; }

    [Column("substitute_id")]
    public int? SubstituteId { get; set; }

    [Required]
    [Column("created_at")]
    public long CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [Column("team_color", TypeName = "TEXT")]
    public string? TeamColor { get; set; }

    [Column("big_image_url", TypeName = "TEXT")]
    public string? BigImageUrl { get; set; }

    public Person? Captain { get; set; } = null!;

    public Person? NonCaptain { get; set; } = null!;

    public Person? Substitute { get; set; } = null!;

    public List<PlayerGameStats> PlayerGameStats { get; set; } = [];

    public List<TournamentTeam> TournamentTeams { get; set; } = [];

    [NotMapped]
    public List<Person> People => new List<Person?> {Captain, NonCaptain, Substitute}
        .Where(p => p != null)
        .Select(p => p!)
        .ToList();


    public TeamData ToSendableData(bool generateStats = false,
        bool generatePlayerStats = false, bool formatData = false, Tournament? tournament = null) {
        return new TeamData(this, tournament, generateStats, generatePlayerStats, formatData);
    }

    public double Elo() => new[] {Captain?.Elo(), NonCaptain?.Elo(), Substitute?.Elo()}.Where(e => e.HasValue)
        .Select(e => e!.Value).DefaultIfEmpty(1500.0).Average();

    public GameTeamData ToGameSendableData(Game game, bool generateStats = false,
        bool formatData = false, bool isAdmin = false) {
        if (Id == 1) {
            return new GameTeamData(this, game, false, false, formatData);
        }

        return new GameTeamData(this, game, generateStats, formatData, isAdmin);
    }

    public static IQueryable<Team> GetRelevant(IQueryable<Team> query) {
        return query
            .Include(t => t.Captain)
            .ThenInclude(p => p.PlayerGameStats.OrderByDescending(pgs => pgs.Id).Take(1))
            .Include(t => t.NonCaptain)
            .ThenInclude(p => p.PlayerGameStats.OrderByDescending(pgs => pgs.Id).Take(1))
            .Include(t => t.Substitute)
            .ThenInclude(p => p.PlayerGameStats.OrderByDescending(pgs => pgs.Id).Take(1));
    }
}