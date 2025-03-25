using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;

namespace HandballBackend.Database.Models;

[Table("people", Schema = "main")]
public class Person {
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

    [Column("password", TypeName = "TEXT")]
    public string? Password { get; set; }

    [Column("image_url", TypeName = "TEXT")]
    public string? ImageUrl { get; set; }

    [Column("session_token", TypeName = "TEXT")]
    public string? SessionToken { get; set; }

    [Column("token_timeout")]
    public int? TokenTimeout { get; set; }

    [Required]
    [Column("created_at")]
    public int? CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [Required]
    [Column("permission_level")]
    public int PermissionLevel { get; set; } = 0;

    public IEnumerable<PlayerGameStats>? PlayerGameStats { get; set; }
    
    public IEnumerable<GameEvent>? Events { get; set; }

    public PersonData ToSendableData(Tournament? tournament = null, bool generateStats = false, Team? team = null,
        bool formatData = false) {
        return new PersonData(this, tournament, generateStats, team, formatData);
    }
}