using HandballBackend.Database.Models;
using HandballBackend.Utils;

namespace HandballBackend.Database.SendableTypes;

public class DocumentData(Document document, bool includeTournament = false) {
    public string Name { get; set; } = document.Name;

    public string Description { get; set; } = document.Description;

    public TournamentData? Tournament { get; set; } = includeTournament ? document.Tournament?.ToSendableData() : null;

    public string Link { get; set; } = Utilities.FixHandballUrl(document.Link);

    public Document.DocumentType Type { get; set; } = document.Type;

    public string ImageUrl { get; set; } = document.ImageUrl ?? ImageOf(document.Type);

    private static string ImageOf(Document.DocumentType documentType) {
        switch (documentType) {
            case Document.DocumentType.Rules:
                return "/api/image?name=umpire&big=true";
            case Document.DocumentType.UmpireQualificationProgram:
                break;
            case Document.DocumentType.TournamentRegulations:
                break;
            case Document.DocumentType.Other:
                return "/api/image?name=SUSS_dark";
            default:
                throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null);
        }
    }
}