using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;

namespace HandballBackend.Database.Models;

[Table("quoteOfTheDay")]
public class QuoteOfTheDay {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("quote", TypeName = "TEXT")]
    public string Quote { get; set; }

    [Required]
    [Column("author", TypeName = "TEXT")]
    public string Author { get; set; }

    public QuoteOfTheDayData ToSendableData() {
        return new QuoteOfTheDayData(this);
    }
}