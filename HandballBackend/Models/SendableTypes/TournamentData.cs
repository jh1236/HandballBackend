namespace HandballBackend.Models.SendableTypes;
// ReSharper disable InconsistentNaming
// Disabled as these are sent to the frontend; we don't care too much about 
//TODO: editable
public record TournamentData
{
        public string name;
        public string searchableName;
        public bool editable;
        public string fixturesType;
        public string finalsType;
        public bool ranked;
        public bool twoCourts;
        public bool hasScorer;
        public bool finished;
        public bool inFinals;
        public bool isPooled;
        public string notes;
        public string imageURL;
        public bool usingBadmintonServes;

        public TournamentData(Tournament tournament, bool isAdmin = false)
        {
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
                imageURL = tournament.ImageUrl;
                usingBadmintonServes = tournament.BadmintonServes;

        }
}