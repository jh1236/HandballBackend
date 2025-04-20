using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.Utils;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.Database.Models;

[Table("officials", Schema = "main")]
public class Official : IHasRelevant<Official> {
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
    public long CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [ForeignKey("PersonId")]
    public Person Person { get; set; }

    public List<Game> Games { get; set; } = new List<Game>();
    
    public OfficialData ToSendableData(Tournament? tournament = null, bool includeStats = false) {
        return new OfficialData(this, tournament, includeStats);
    }

    public static IQueryable<Official> GetRelevant(IQueryable<Official> query) {
        return query.Include(o => o.Person);
    }
}