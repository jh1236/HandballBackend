using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.Models;

[Table("tournamentTeams", Schema = "main")]
public class TournamentTeam : IHasRelevant<TournamentTeam> {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("tournament_id")]
    public int TournamentId { get; set; }

    [Required]
    [Column("team_id")]
    public int TeamId { get; set; }

    [Column("pool")]
    public int? Pool { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [Column("name", TypeName = "TEXT")]
    public string? Name { get; set; }

    [Column("long_name", TypeName = "TEXT")]
    public string? LongName { get; set; }

    [Column("image_url", TypeName = "TEXT")]
    public string? ImageUrl { get; set; }

    [Column("team_color", TypeName = "TEXT")]
    public string? TeamColor { get; set; }

    [Column("big_image_url", TypeName = "TEXT")]
    public string? BigImageUrl { get; set; }

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    [ForeignKey("TeamId")]
    public Team Team { get; set; }

    public TeamData ToSendableData(bool generateStats = false,
        bool generatePlayerStats = false, bool formatData = false) {
        return new TournamentTeamData(this, generateStats, generatePlayerStats, formatData);
    }

    public static IQueryable<TournamentTeam> GetRelevant(IQueryable<TournamentTeam> query) {
        return query
            .Include(t => t.Team.Captain)
            .ThenInclude(p => p.PlayerGameStats.OrderByDescending(pgs => pgs.Id).Take(1))
            .Include(t => t.Team.NonCaptain)
            .ThenInclude(p => p.PlayerGameStats.OrderByDescending(pgs => pgs.Id).Take(1))
            .Include(t => t.Team.Substitute)
            .ThenInclude(p => p.PlayerGameStats.OrderByDescending(pgs => pgs.Id).Take(1));
    }
}