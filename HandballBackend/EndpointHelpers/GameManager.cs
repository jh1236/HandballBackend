using HandballBackend.Database;
using Microsoft.EntityFrameworkCore;

namespace HandballBackend.EndpointHelpers;

public static class GameManager {
    public static void StartGame(int gameNumber, bool swapService, string[]? playersTeamOne, string[]? playersTeamTwo,
        bool teamOneIsIGa, string officialSearchable, string scorerSearchable) {
        var db = new HandballContext();
        var game = db.Games.IncludeRelevant().Include(g => g.Events).FirstOrDefault(g => g.GameNumber == gameNumber);
        if (game == null) throw new ArgumentException("The game number provided doesn't exist");
        if (game.Started) throw new InvalidOperationException("The game has already begun");
        int[] teamOneIds = [];
        int[] teamTwoIds = [];
        if (playersTeamOne == null) {
            teamOneIds = game.Players.Where(pgs => pgs.TeamId == game.TeamOneId).Select(a => a.PlayerId).ToArray();
        } else {
            var output = new List<int>();
            foreach (var searchableName in playersTeamOne) {
                output.Add(game.Players.SingleOrDefault(pgs => pgs.Player.SearchableName == searchableName).PlayerId));
            }
            teamOneIds = output.ToArray();
        }
        if (playersTeamTwo == null) {
            playersTeamTwo = game.Players.Where(pgs => pgs.TeamId == game.TeamTwoId).Select(a => a.Player.SearchableName).ToArray();
        } else {
            var output = new List<int>();
            foreach (var searchableName in playersTeamTwo) {
                output.Add(game.Players.SingleOrDefault(pgs => pgs.Player.SearchableName == searchableName).PlayerId));
            }
            teamTwoIds = output.ToArray();
        }
        if(teamOneIds)
    }
}