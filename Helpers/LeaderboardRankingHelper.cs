using FootballPredictionGame.Models;
using FootballPredictionGame.ViewModels;

namespace FootballPredictionGame.Helpers;

public static class LeaderboardRankingHelper
{
    public static List<LeaderboardUserViewModel> Build(IEnumerable<ApplicationUser> users, int? take = null, bool scoredOnly = false)
    {
        var orderedUsers = users
            .Where(x => x.IsActive)
            .Where(x => !scoredOnly || x.TotalScore > 0)
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.ExactPredictionCount)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        var result = new List<LeaderboardUserViewModel>();
        int denseRank = 0;
        int? currentRank = null;
        int? previousScore = null;

        foreach (var user in orderedUsers)
        {
            int? rank = null;
            if (user.TotalScore > 0)
            {
                if (previousScore != user.TotalScore)
                {
                    denseRank++;
                    currentRank = denseRank;
                    previousScore = user.TotalScore;
                }

                rank = currentRank;
            }

            result.Add(new LeaderboardUserViewModel
            {
                Rank = rank,
                UserId = user.Id,
                FullName = user.FullName,
                PhotoPath = user.ProfilePhotoPath,
                TotalScore = user.TotalScore,
                ExactPredictionCount = user.ExactPredictionCount,
                CreatedAt = user.CreatedAt
            });
        }

        return take.HasValue ? result.Take(take.Value).ToList() : result;
    }
}
