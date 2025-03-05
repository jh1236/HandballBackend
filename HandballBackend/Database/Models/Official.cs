using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Models;

namespace HandballBackend.Database.Models;

[Table("officials", Schema = "main")]
public class Official {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("person_id")]
    public int PersonId { get; set; }

    [Required]
    [Column("proficiency")]
    public int Proficiency { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [ForeignKey("PersonId")]
    public Person Person { get; set; }

    public OfficialData ToSendableData( Tournament? tournament = null, bool includeStats = false) {
        return new OfficialData(this);
    }
}