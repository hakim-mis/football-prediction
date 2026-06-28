using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;

namespace FootballPredictionGame.Helpers;

public static class LeaderboardRankingHelper
{
    public static List<LeaderboardUserViewModel> Build(
        IEnumerable<ApplicationUser> users,
        int? take = null,
        bool scoredOnly = false,
        Dictionary<string, int>? winMatchPredictionCountMap = null)
    {
        winMatchPredictionCountMap ??= new Dictionary<string, int>();

        var orderedUsers = users
            .Where(x => x.IsActive)
            .Where(x => !scoredOnly || x.TotalScore > 0)
            .Select(user => new LeaderboardUserViewModel
            {
                Rank = null,
                UserId = user.Id,
                FullName = user.FullName,
                Designation = user.Designation,
                Department = user.Department,
                PhotoPath = user.ProfilePhotoPath,
                TotalScore = user.TotalScore,
                ExactPredictionCount = user.ExactPredictionCount,
                WinMatchPredictionCount = winMatchPredictionCountMap.TryGetValue(user.Id, out var winCount)
                    ? winCount
                    : 0,
                CreatedAt = user.CreatedAt
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.ExactPredictionCount)
            .ThenByDescending(x => x.WinMatchPredictionCount)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.FullName)
            .ToList();

        var denseRank = 0;
        int? currentRank = null;
        int? previousScore = null;

        foreach (var user in orderedUsers)
        {
            if (user.TotalScore <= 0)
            {
                user.Rank = null;
                continue;
            }

            if (previousScore != user.TotalScore)
            {
                denseRank++;
                currentRank = denseRank;
                previousScore = user.TotalScore;
            }

            user.Rank = currentRank;
        }

        return take.HasValue
            ? orderedUsers.Take(take.Value).ToList()
            : orderedUsers;
    }
}