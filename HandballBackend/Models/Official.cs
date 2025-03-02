using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandballBackend.Models {
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
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PersonId")]
        public Person Person { get; set; }
    }
}