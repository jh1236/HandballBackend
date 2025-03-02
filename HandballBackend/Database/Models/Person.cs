using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;

namespace HandballBackend.Models;

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
    public string Password { get; set; }

    [Column("image_url", TypeName = "TEXT")]
    public string ImageUrl { get; set; }

    [Column("session_token", TypeName = "TEXT")]
    public string SessionToken { get; set; }

    [Column("token_timeout")]
    public int? TokenTimeout { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Required]
    [Column("permission_level")]
    public int PermissionLevel { get; set; } = 0;

    public PersonData ToSendableData() {
        return new PersonData(this);
    }
}