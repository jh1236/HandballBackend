using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Utils;

namespace HandballBackend.Database.Models;

[Table("eloChange", Schema = "main")]
public class EloChange {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("game_id")]
    public int GameId { get; set; }


    [Required]
    [Column("player_id")]
    public int PlayerId { get; set; }


    [Column("tournament_id")]
    public int? TournamentId { get; set; }


    [Required]
    [Column("elo_delta")]
    public int EloDelta { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = Utilities.GetUnixSeconds();


    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    [ForeignKey("GameId")]
    public Game Game { get; set; }

    [ForeignKey("PlayerId")]
    public Person Player { get; set; }
}