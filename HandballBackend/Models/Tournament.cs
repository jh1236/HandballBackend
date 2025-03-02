using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
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
        public int Ranked { get; set; }

        [Required]
        [Column("two_courts")]
        public int TwoCourts { get; set; }

        [Required]
        [Column("finished")]
        public int Finished { get; set; } = 0;

        [Required]
        [Column("in_finals")]
        public int InFinals { get; set; } = 0;

        [Required]
        [Column("has_scorer")]
        public int HasScorer { get; set; } = 1;

        [Required]
        [Column("is_pooled")]
        public int IsPooled { get; set; } = 0;

        [Column("notes", TypeName = "TEXT")]
        public string Notes { get; set; }

        [Column("image_url", TypeName = "TEXT")]
        public string ImageUrl { get; set; }

        [Required]
        [Column("created_at")]
        public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [Required]
        [Column("badminton_serves")]
        public int BadmintonServes { get; set; } = 0;
    }
}