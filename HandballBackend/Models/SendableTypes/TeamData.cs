// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 

using System.Drawing;

namespace HandballBackend.Models.SendableTypes;

public record TeamData {
    public readonly string name;
    public readonly string searchableName;
    public readonly string? imageUrl;
    public readonly string? bigImageUrl;
    public readonly PersonData? captain;
    public readonly PersonData? nonCaptain;
    public readonly PersonData? substitute;
    public readonly string? teamColor;
    public readonly int[]? teamColorAsRGBABecauseDigbyIsLazy;
    public readonly float elo = 1500.0f;

    private static int[] GenerateRgba(string backgroundColor) {
        var color = ColorTranslator.FromHtml(backgroundColor);
        int r = Convert.ToInt16(color.R);
        int g = Convert.ToInt16(color.G);
        int b = Convert.ToInt16(color.B);
        return [r, g, b, 255];
    }

    public TeamData(Team team) {
        name = team.Name;
        searchableName = team.SearchableName;
        imageUrl = team.ImageUrl;
        bigImageUrl = team.BigImageUrl;
        captain = team?.Captain?.ToSendableData();
        nonCaptain = team?.NonCaptain?.ToSendableData();
        substitute = team?.Substitute?.ToSendableData();
        teamColor = team?.TeamColor;
        teamColorAsRGBABecauseDigbyIsLazy = teamColor != null ? GenerateRgba(teamColor) : null;
    }
}