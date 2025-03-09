using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.Models;

[Table("teams", Schema = "main")]
public class Team : IHasRelevant<Team> {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("name", TypeName = "TEXT")]
    public string Name { get; set; }

    [Required]
    [Column("searchable_name", TypeName = "TEXT")]
    public string SearchableName { get; set; }

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
    public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Column("team_color", TypeName = "TEXT")]
    public string? TeamColor { get; set; }

    [Column("big_image_url", TypeName = "TEXT")]
    public string? BigImageUrl { get; set; }

    public Person? Captain { get; set; } = null!;

    public Person? NonCaptain { get; set; } = null!;

    public Person? Substitute { get; set; } = null!;

    public List<PlayerGameStats> PlayerGameStats { get; set; } = [];

    public TeamData ToSendableData(Tournament? tournament = null, bool generateStats = false,
        bool generatePlayerStats = false, bool formatData = false) {
        return new TeamData(this, tournament, generateStats, generatePlayerStats, formatData);
    }

    public GameTeamData ToGameSendableData(Game game, bool generateStats = false,
        bool formatData = false) {
        return new GameTeamData(this, game, generateStats, formatData);
    }

    public static IQueryable<Team> GetRelevant(IQueryable<Team> query) {
        return query
            .Include(t => t.Captain)
            .Include(t => t.NonCaptain)
            .Include(t => t.Substitute);
    }
}