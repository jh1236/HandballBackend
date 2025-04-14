// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using System.Drawing;
using HandballBackend.Database.Models;
using HandballBackend.Utils;

namespace HandballBackend.Database.SendableTypes;

public class TournamentTeamData : TeamData {
    public TournamentTeamData(TournamentTeam tt, bool generateStats = false,
        bool generatePlayerStats = false, bool formatData = false) : base(tt.Team, tt.Tournament, generateStats,
        generatePlayerStats, formatData) {
        imageUrl = tt.ImageUrl == null ? imageUrl : Utilities.FixImageUrl(tt.ImageUrl);
        imageUrl = tt.BigImageUrl == null ? bigImageUrl : Utilities.FixImageUrl(tt.BigImageUrl);
        name = tt.Name ?? name;
    }
}