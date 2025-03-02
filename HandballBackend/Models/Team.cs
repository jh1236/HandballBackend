using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
    [Table("teams", Schema = "main")]
    public class Team {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("name", TypeName = "TEXT")]
        public string Name { get; set; }

        [Required]
        [Column("searchable_name", TypeName = "TEXT")]
        public string SearchableName { get; set; }

        [Column("image_url", TypeName = "TEXT")]
        public string ImageUrl { get; set; }

        [Column("captain_id")]
        public int? CaptainId { get; set; }


        [Column("non_captain_id")]
        public int? NonCaptainId { get; set; }

        [Column("substitute_id")]
        public int? SubstituteId { get; set; }

        [Required]
        [Column("created_at")]
        public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        [Column("team_color", TypeName = "TEXT")]
        public string TeamColor { get; set; }

        [Column("big_image_url", TypeName = "TEXT")]
        public string BigImageUrl { get; set; }

        [ForeignKey("CaptainId")]
        public Person Captain { get; set; }

        [ForeignKey("NonCaptainId")]
        public Person NonCaptain { get; set; }

        [ForeignKey("SubstituteId")]
        public Person Substitute { get; set; }
    }
}