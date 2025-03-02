using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
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
        public DateTime CreatedAt { get; set; } = DateTime.Now;


        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }

        [ForeignKey("GameId")]
        public Game Game { get; set; }

        [ForeignKey("PlayerId")]
        public Person Player { get; set; }
    }
}