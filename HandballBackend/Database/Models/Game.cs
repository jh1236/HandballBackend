using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.Models;

[Table("games", Schema = "main")]
public class Game : IHasRelevant<Game> {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("tournament_id")]
    public int TournamentId { get; set; }

    [Required]
    [Column("team_one_id")]
    public int TeamOneId { get; set; }


    [Required]
    [Column("team_two_id")]
    public int TeamTwoId { get; set; }


    [Required]
    [Column("team_one_score")]
    public int TeamOneScore { get; set; } = 0;

    [Required]
    [Column("team_two_score")]
    public int TeamTwoScore { get; set; } = 0;

    [Required]
    [Column("team_one_timeouts")]
    public int TeamOneTimeouts { get; set; } = 0;

    [Required]
    [Column("team_two_timeouts")]
    public int TeamTwoTimeouts { get; set; } = 0;

    [Column("winning_team_id")]
    public int? WinningTeamId { get; set; }

    [Required]
    [Column("started")]
    public bool Started { get; set; } = false;

    [Required]
    [Column("ended")]
    public bool Ended { get; set; } = false;

    [Required]
    [Column("someone_has_won")]
    public bool SomeoneHasWon { get; set; }

    [Required]
    [Column("protested")]
    public bool Protested { get; set; }

    [Required]
    [Column("resolved")]
    public bool Resolved { get; set; }

    [Required]
    [Column("ranked")]
    public bool Ranked { get; set; } = true;

    [Column("best_player_id")]
    public int? BestPlayerId { get; set; }

    [Column("official_id")]
    public int? OfficialId { get; set; }

    [Column("scorer_id")]
    public int? ScorerId { get; set; }

    [Column("iga_side_id")]
    public int? IgaSideId { get; set; }

    [Column("player_to_serve_id")]
    public int? PlayerToServeId { get; set; }

    [Column("team_to_serve_id")]
    public int? TeamToServeId { get; set; }

    [Column("side_to_serve", TypeName = "TEXT")]
    public string? SideToServe { get; set; }

    [Column("start_time")]
    public int? StartTime { get; set; }

    [Column("length")]
    public int? Length { get; set; }

    [Required]
    [Column("court")]
    public int Court { get; set; } = 0;

    [Required]
    [Column("is_final")]
    public bool IsFinal { get; set; }

    [Required]
    [Column("round")]
    public int Round { get; set; }

    [Column("notes", TypeName = "TEXT")]
    public string? Notes { get; set; }

    [Required]
    [Column("is_bye")]
    public bool IsBye { get; set; }

    [Required]
    [Column("status", TypeName = "TEXT")]
    public string Status { get; set; } = "Waiting For Start";

    [Column("admin_status", TypeName = "TEXT")]
    public string AdminStatus { get; set; } = "Waiting For Start";

    [Required]
    [Column("noteable_status", TypeName = "TEXT")]
    public string NoteableStatus { get; set; } = "Waiting For Start";

    [Column("created_at")]
    public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Column("serve_timer")]
    public int? ServeTimer { get; set; }

    [Required]
    [Column("marked_for_review")]
    public int MarkedForReview { get; set; } = 0;

    [Required]
    [Column("game_number")]
    public int GameNumber { get; set; }

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    [ForeignKey("TeamOneId")]
    public Team TeamOne { get; set; }

    [ForeignKey("TeamTwoId")]
    public Team TeamTwo { get; set; }

    [ForeignKey("BestPlayerId")]
    public Person? BestPlayer { get; set; }

    [ForeignKey("OfficialId")]
    public Official? Official { get; set; }

    [ForeignKey("ScorerId")]
    public Official? Scorer { get; set; }

    public ICollection<GameEvent> Events { get; set; } = new List<GameEvent>();

    public ICollection<PlayerGameStats> Players { get; set; } = new List<PlayerGameStats>();

    public GameData ToSendableData(
        bool includeGameEvents = false,
        bool includeStats = false,
        bool formatData = false,
        bool isAdmin = false
    ) {
        return new GameData(this,includeGameEvents, includeStats, formatData, isAdmin);
    }

    public static IQueryable<Game> GetRelevant(IQueryable<Game> query) {
        return query
            .Include(x => x.Tournament)
            .Include(x => x.TeamOne.Captain)
            .Include(x => x.TeamOne.NonCaptain)
            .Include(x => x.TeamOne.Substitute)
            .Include(x => x.TeamTwo.Captain)
            .Include(x => x.TeamTwo.NonCaptain)
            .Include(x => x.TeamTwo.Substitute)
            .Include(x => x.Tournament)
            .Include(x => x.BestPlayer)
            .Include(x => x.Official.Person)
            .Include(x => x.Scorer.Person)
            .Include(x => x.Players);
    }
}