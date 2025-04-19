using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HandballBackend.Database.SendableTypes;
using HandballBackend.EndpointHelpers;
using HandballBackend.FixtureGenerator;
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
    [Column("text_alerts")]
    public bool TextAlerts { get; set; } = true;

    [Required]
    [Column("is_pooled")]
    public bool IsPooled { get; set; } = false;

    [Column("notes", TypeName = "TEXT")]
    public string? Notes { get; set; }

    [Column("image_url", TypeName = "TEXT")]
    public string ImageUrl { get; set; }

    [Required]
    [Column("created_at")]
    public int CreatedAt { get; set; } = Utilities.GetUnixSeconds();

    [Required]
    [Column("badminton_serves")]
    public bool BadmintonServes { get; set; } = false;

    public void EndRound() {
        var finals = InFinals;
        if (!finals) {
            finals = GetFixtureGenerator.EndOfRound();
        }

        var finished = false;
        if (finals && !Finished) {
            finished = GetFinalGenerator.EndOfRound();
        }

        if (!TextAlerts || finished) return;
        
        var db = new HandballContext();
        for (var i = 0; i < (TwoCourts ? 2 : 1); i++) {
            var nextGame = db.Games
                .Where(g => g.TournamentId == Id && !g.IsBye && !g.Started && g.Court == i)
                .IncludeRelevant()
                .OrderBy(g => g.Id).FirstOrDefault();
            TextHelper.Text(nextGame.Official.Person,
                $"You are umpiring the game between {nextGame.TeamOne.Name} and {nextGame.TeamTwo.Name} on court {nextGame.Court + 1}.");
            if (nextGame.ScorerId != null && nextGame.ScorerId != nextGame.OfficialId) {
                TextHelper.Text(nextGame.Official.Person,
                    $"You are scoring the game between {nextGame.TeamOne.Name} and {nextGame.TeamTwo.Name} on court {nextGame.Court + 1}.");
            }

            var teams = new[] {nextGame.TeamOne, nextGame.TeamTwo};
            for (var j = 0; j < teams.Length; j++) {
                var team = teams[j];
                var oppTeam = teams[1 - j];
                TextHelper.Text(team.Captain,
                    $"Your game against {oppTeam.Name} is beginning soon on court {nextGame.Court + 1}.");
            }
        }
        
    }

    public void BeginTournament() {
        GetFixtureGenerator.BeginTournament();
    }

    [NotMapped]
    public AbstractFixtureGenerator GetFinalGenerator => AbstractFixtureGenerator.GetControllerByName(FinalsType, Id);

    [NotMapped]
    public AbstractFixtureGenerator GetFixtureGenerator =>
        AbstractFixtureGenerator.GetControllerByName(FixturesType, Id);

    public TournamentData ToSendableData() {
        return new TournamentData(this);
    }
}