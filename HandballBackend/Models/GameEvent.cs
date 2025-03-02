using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
    [Table("gameEvents", Schema = "main")]
    public class GameEvent {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("game_id")]
        public int GameId { get; set; }

        [Column("player_id")]
        public int? PlayerId { get; set; }

        [Column("team_id")]
        public int? TeamId { get; set; }

        [Required]
        [Column("tournament_id")]
        public int TournamentId { get; set; }

        [Required]
        [Column("event_type", TypeName = "TEXT")]
        public string EventType { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("details")]
        public int? Details { get; set; }

        [Column("notes", TypeName = "TEXT")]
        public string Notes { get; set; }

        [Column("player_who_served_id")]
        public int? PlayerWhoServedId { get; set; }

        [Column("team_who_served_id")]
        public int? TeamWhoServedId { get; set; }

        [Column("side_served", TypeName = "TEXT")]
        public string SideServed { get; set; }

        [Column("player_to_serve_id")]
        public int? PlayerToServeId { get; set; }

        [Column("team_to_serve_id")]
        public int? TeamToServeId { get; set; }

        [Column("side_to_serve", TypeName = "TEXT")]
        public string SideToServe { get; set; }

        [Column("team_one_left_id")]
        public int? TeamOneLeftId { get; set; }
        
        [Column("team_one_right_id")]
        public int? TeamOneRightId { get; set; }
        
        [Column("team_two_left_id")]
        public int? TeamTwoLeftId { get; set; }

        [Column("team_two_right_id")]
        public int? TeamTwoRightId { get; set; }
        
        [ForeignKey("PlayerId")]
        public Person Player { get; set; }

        [ForeignKey("TeamId")]
        public Team Team { get; set; }

        [ForeignKey("TournamentId")]
        public Tournament Tournament { get; set; }

        [ForeignKey("PlayerWhoServedId")]
        public Person PlayerWhoServed { get; set; }

        [ForeignKey("TeamWhoServedId")]
        public Team TeamWhoServed { get; set; }

        [ForeignKey("PlayerToServeId")]
        public Person PlayerToServe { get; set; }

        [ForeignKey("TeamToServeId")]
        public Team TeamToServe { get; set; }

        [ForeignKey("TeamOneLeftId")]
        public Person TeamOneLeft { get; set; }

        [ForeignKey("TeamOneRightId")]
        public Person TeamOneRight { get; set; }
        
        [ForeignKey("TeamTwoLeftId")]
        public Person TeamTwoLeft { get; set; }
        
        [ForeignKey("GameId")]
        public Game Game { get; set; }
        
        [ForeignKey("TeamTwoRightId")]
        public Person TeamTwoRight { get; set; }
    }
}