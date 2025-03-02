using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
    [Table("games", Schema = "main")]
    public class Game {
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
        public int Started { get; set; } = 0;

        [Required]
        [Column("ended")]
        public int Ended { get; set; } = 0;

        [Required]
        [Column("someone_has_won")]
        public int SomeoneHasWon { get; set; } = 0;

        [Required]
        [Column("protested")]
        public int Protested { get; set; } = 0;

        [Required]
        [Column("resolved")]
        public int Resolved { get; set; } = 0;

        [Required]
        [Column("ranked")]
        public int Ranked { get; set; } = 1;

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
        public string SideToServe { get; set; }

        [Column("start_time")]
        public int? StartTime { get; set; }

        [Column("length")]
        public int? Length { get; set; }

        [Required]
        [Column("court")]
        public int Court { get; set; } = 0;

        [Required]
        [Column("is_final")]
        public int IsFinal { get; set; } = 0;

        [Required]
        [Column("round")]
        public int Round { get; set; }

        [Column("notes", TypeName = "TEXT")]
        public string Notes { get; set; }

        [Required]
        [Column("is_bye")]
        public int IsBye { get; set; } = 0;

        [Required]
        [Column("status", TypeName = "TEXT")]
        public string Status { get; set; } = "Waiting For Start";

        [Column("admin_status", TypeName = "TEXT")]
        public string AdminStatus { get; set; } = "Waiting For Start";

        [Required]
        [Column("noteable_status", TypeName = "TEXT")]
        public string NoteableStatus { get; set; } = "Waiting For Start";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

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

        [ForeignKey("WinningTeamId")]
        public Team WinningTeam { get; set; }

        [ForeignKey("BestPlayerId")]
        public Person BestPlayer { get; set; }

        [ForeignKey("OfficialId")]
        public Official Official { get; set; }

        [ForeignKey("ScorerId")]
        public Official Scorer { get; set; }

        [ForeignKey("IgaSideId")]
        public Team IgaSide { get; set; }

        [ForeignKey("PlayerToServeId")]
        public Person PlayerToServe { get; set; }

        [ForeignKey("TeamToServeId")]
        public Team TeamToServe { get; set; }
    }
}