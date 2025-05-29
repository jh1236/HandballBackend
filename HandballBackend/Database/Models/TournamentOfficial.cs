using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.Models;

[Table("tournament_officials")]
public class TournamentOfficial : IHasRelevant<TournamentOfficial> {
    [Key]
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
    public bool IsUmpire { get; set; } = true;

    [Required]
    [Column("is_scorer")]
    public bool IsScorer { get; set; } = true;

    [Required]
    [Column("created_at")]
    public long CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [ForeignKey("TournamentId")]
    public Tournament Tournament { get; set; }

    [ForeignKey("OfficialId")]
    public Official Official { get; set; }

    [NotMapped]
    public int GamesUmpired {
        get {
            var db = new HandballContext();
            return db.Games.Count(g => g.TournamentId == TournamentId && g.OfficialId == OfficialId);
        }
    }

    [NotMapped]
    public int CourtOneGamesUmpired {
        get {
            var db = new HandballContext();
            return db.Games.Count(g => g.Court == 0 && g.TournamentId == TournamentId && g.OfficialId == OfficialId);
        }
    }

    [NotMapped]
    public int CourtTwoGamesUmpired {
        get {
            var db = new HandballContext();
            return db.Games.Count(g => g.Court == 1 && g.TournamentId == TournamentId && g.OfficialId == OfficialId);
        }
    }

    [NotMapped]
    public int GamesScored {
        get {
            var db = new HandballContext();
            return db.Games.Count(g => g.TournamentId == TournamentId && g.ScorerId == OfficialId);
        }
    }

    public static IQueryable<TournamentOfficial> GetRelevant(IQueryable<TournamentOfficial> query) {
        return query
            .Include(to => to.Tournament)
            .Include(to => to.Official.Person);
    }
}