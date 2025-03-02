using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
    [Table("tournamentOfficials", Schema = "main")]
    public class TournamentOfficial {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("tournament_id")]
        public int TournamentId { get; set; }

        [Required]
        [Column("official_id")]
        public int OfficialId { get; set; }

        [Required]
        [Column("is_umpire")]
        public int IsUmpire { get; set; } = 1;

        [Required]
        [Column("is_scorer")]
        public int IsScorer { get; set; } = 1;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }

        [ForeignKey("OfficialId")]
        public Official Official { get; set; }
    }
}