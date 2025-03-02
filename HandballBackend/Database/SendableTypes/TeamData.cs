// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using System.Drawing;
using HandballBackend.Database.Models;
using HandballBackend.Models;

namespace HandballBackend.Database.SendableTypes;

public class TeamData {
    public string name { get; private set; }
    public string searchableName { get; private set; }
    public string? imageUrl { get; private set; }
    public string? bigImageUrl { get; private set; }
    public PersonData? captain { get; private set; }
    public PersonData? nonCaptain { get; private set; }
    public PersonData? substitute { get; private set; }
    public string? teamColor { get; private set; }
    public int[]? teamColorAsRGBABecauseDigbyIsLazy { get; private set; }
    public float elo { get; private set; }

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
        captain = team.Captain?.ToSendableData();
        nonCaptain = team.NonCaptain?.ToSendableData();
        substitute = team.Substitute?.ToSendableData();
        teamColor = team?.TeamColor;
        teamColorAsRGBABecauseDigbyIsLazy = teamColor != null ? GenerateRgba(teamColor) : null;
        elo = 1500.0f;
    }
}