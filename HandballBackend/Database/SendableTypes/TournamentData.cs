// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about the cs naming conventions

using HandballBackend.Database.Models;
using HandballBackend.Utils;

namespace HandballBackend.Database.SendableTypes;

//TODO: editable
public class TournamentData {
    public string name { get; private set; }
    public string searchableName { get; private set; }
    public bool editable { get; private set; }
    public string fixturesType { get; private set; }
    public string finalsType { get; private set; }
    public bool ranked { get; private set; }
    public bool twoCourts { get; private set; }
    public bool hasScorer { get; private set; }
    public bool finished { get; private set; }
    public bool inFinals { get; private set; }
    public bool isPooled { get; private set; }
    public string notes { get; private set; }
    public string ImageUrl { get; private set; }
    public bool usingBadmintonServes { get; private set; }

    public TournamentData(Tournament tournament, bool isAdmin = false) {
        name = tournament.Name;
        searchableName = tournament.SearchableName;
        fixturesType = tournament.FixturesType;
        finalsType = tournament.FinalsType;
        ranked = tournament.Ranked;
        twoCourts = tournament.TwoCourts;
        hasScorer = tournament.HasScorer;
        finished = tournament.Finished;
        inFinals = tournament.InFinals;
        isPooled = tournament.IsPooled;
        notes = tournament.Notes;
        ImageUrl = Utilities.FixImageUrl(tournament.ImageUrl);
        usingBadmintonServes = tournament.BadmintonServes;
    }
}