using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
    [Table("tournamentTeams", Schema = "main")]
    public class TournamentTeam {
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
        public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [Column("name", TypeName = "TEXT")]
        public string Name { get; set; }

        [Column("image_url", TypeName = "TEXT")]
        public string ImageUrl { get; set; }

        [Column("team_color", TypeName = "TEXT")]
        public string TeamColor { get; set; }

        [Column("big_image_url", TypeName = "TEXT")]
        public string BigImageUrl { get; set; }

        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }

        [ForeignKey("TeamId")]
        public Team Team { get; set; }
    }
}