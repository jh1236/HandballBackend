using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;

namespace HandballBackend.Database.Models;

[Table("tournaments", Schema = "main")]
public class Tournament {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name", TypeName = "TEXT")]
    public string Name { get; set; }

    [Required]
    [Column("searchable_name", TypeName = "TEXT")]
    public string SearchableName { get; set; }

    [Required]
    [Column("fixtures_type", TypeName = "TEXT")]
    public string FixturesType { get; set; }

    [Column("finals_type", TypeName = "TEXT")]
    public string FinalsType { get; set; }

    [Required]
    [Column("ranked")]
    public bool Ranked { get; set; }

    [Required]
    [Column("two_courts")]
    public bool TwoCourts { get; set; }

    [Required]
    [Column("finished")]
    public bool Finished { get; set; } = false;

    [Required]
    [Column("in_finals")]
    public bool InFinals { get; set; } = false;

    [Required]
    [Column("has_scorer")]
    public bool HasScorer { get; set; } = true;

    [Required]
    [Column("is_pooled")]
    public bool IsPooled { get; set; } = false;

    [Column("notes", TypeName = "TEXT")]
    public string Notes { get; set; }

    [Column("image_url", TypeName = "TEXT")]
    public string ImageUrl { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [Required]
    [Column("badminton_serves")]
    public bool BadmintonServes { get; set; } = false;

    public TournamentData ToSendableData() {
        return new TournamentData(this);
    }
}