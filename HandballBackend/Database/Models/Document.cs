using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;

namespace HandballBackend.Database.Models;

[Table("documents")]
public class Document {
    public enum DocumentType {
        UmpireQualificationProgram,
        Rules,
        TournamentRegulations
    }

    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    public required string Name { get; set; }

    [Required]
    [Column("description")]
    public required string Description { get; set; }

    [Required]
    [Column("type")]
    public required DocumentType Type { get; set; }

    [Required]
    [Column("link")]
    public required string Link { get; set; }

    [Column("tournament_id")]
    public int? TournamentId { get; set; } = null;


    [ForeignKey("TournamentId")]
    public Tournament? Tournament { get; set; } = null;
}