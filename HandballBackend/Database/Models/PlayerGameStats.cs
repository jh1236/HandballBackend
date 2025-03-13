﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Models;

namespace HandballBackend.Database.Models;

[Table("playerGameStats", Schema = "main")]
public class PlayerGameStats {
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

    [Required]
    [Column("team_id")]
    public int TeamId { get; set; }

    [Required]
    [Column("opponent_id")]
    public int? OpponentId { get; set; }

    [Required]
    [Column("tournament_id")]
    public int TournamentId { get; set; }

    [Required]
    [Column("rounds_on_court")]
    public int RoundsOnCourt { get; set; } = 0;

    [Required]
    [Column("rounds_carded")]
    public int RoundsCarded { get; set; } = 0;

    [Required]
    [Column("points_scored")]
    public int PointsScored { get; set; } = 0;

    [Required]
    [Column("aces_scored")]
    public int AcesScored { get; set; } = 0;

    [Required]
    [Column("faults")]
    public int Faults { get; set; } = 0;

    [Required]
    [Column("served_points")]
    public int ServedPoints { get; set; } = 0;

    [Required]
    [Column("served_points_won")]
    public int ServedPointsWon { get; set; } = 0;

    [Required]
    [Column("serves_received")]
    public int ServesReceived { get; set; } = 0;

    [Required]
    [Column("serves_returned")]
    public int ServesReturned { get; set; } = 0;

    [Required]
    [Column("double_faults")]
    public int DoubleFaults { get; set; } = 0;

    [Required]
    [Column("green_cards")]
    public int GreenCards { get; set; } = 0;

    [Required]
    [Column("warnings")]
    public int Warnings { get; set; } = 0;

    [Required]
    [Column("yellow_cards")]
    public int YellowCards { get; set; } = 0;

    [Required]
    [Column("red_cards")]
    public int RedCards { get; set; } = 0;

    [Required]
    [Column("card_time_remaining")]
    public int CardTimeRemaining { get; set; } = 0;

    [Required]
    [Column("card_time")]
    public int CardTime { get; set; } = 0;

    [Column("start_side", TypeName = "TEXT")]
    public string StartSide { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Required]
    [Column("is_best_player")]
    public bool IsBestPlayer { get; set; } = false;

    [Required]
    [Column("ace_streak")]
    public int AceStreak { get; set; } = 0;

    [Required]
    [Column("serve_streak")]
    public int ServeStreak { get; set; } = 0;

    [Column("side_of_court")]
    public string? SideOfCourt { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [ForeignKey("GameId")]
    public Game Game { get; set; }

    [ForeignKey("PlayerId")]
    public Person Player { get; set; }

    [ForeignKey("TeamId")]
    public Team Team { get; set; }

    [ForeignKey("OpponentId")]
    public Team Opponent { get; set; }

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    public GamePlayerData ToSendableData(bool includeStats = false, bool formatData = false) {
        return new GamePlayerData(this, includeStats, formatData);
    }

    public void ResetStats() {
        RoundsOnCourt = 0;
        RoundsCarded = 0;
        PointsScored = 0;
        AcesScored = 0;
        Faults = 0;
        DoubleFaults = 0;
        ServedPoints = 0;
        ServedPointsWon = 0;
        ServesReceived = 0;
        ServesReturned = 0;
        Warnings = 0;
        GreenCards = 0;
        YellowCards = 0;
        RedCards = 0;
        CardTime = 0;
        CardTimeRemaining = 0;
        IsBestPlayer = false;
    }
}