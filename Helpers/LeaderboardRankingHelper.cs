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
                WinMatchPredictionCount = winMatchPredictionCountMap.TryGetValue(user.Id, out var winMatchCount)
                    ? winMatchCount
                    : 0,
                CreatedAt = user.CreatedAt
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.ExactPredictionCount)
            .ThenByDescending(x => x.WinMatchPredictionCount)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        var result = new List<LeaderboardUserViewModel>();

        var denseRank = 0;
        int? currentRank = null;

        int? previousScore = null;
        int? previousExact = null;
        int? previousWin = null;

        foreach (var user in orderedUsers)
        {
            int? rank = null;

            if (user.TotalScore > 0)
            {
                var isNewRank =
                    previousScore != user.TotalScore ||
                    previousExact != user.ExactPredictionCount ||
                    previousWin != user.WinMatchPredictionCount;

                if (isNewRank)
                {
                    denseRank++;
                    currentRank = denseRank;

                    previousScore = user.TotalScore;
                    previousExact = user.ExactPredictionCount;
                    previousWin = user.WinMatchPredictionCount;
                }

                rank = currentRank;
            }

            user.Rank = rank;
            result.Add(user);
        }

        return take.HasValue
            ? result.Take(take.Value).ToList()
            : result;
    }

    private static bool IsWinMatchPrediction(Prediction prediction)
    {
        if (prediction.Fixture == null)
        {
            return false;
        }

        if (!prediction.Fixture.TeamOneActualGoal.HasValue ||
            !prediction.Fixture.TeamTwoActualGoal.HasValue)
        {
            return false;
        }

        var predictedResult = Math.Sign(
            prediction.TeamOnePredictedGoal - prediction.TeamTwoPredictedGoal);

        var actualResult = Math.Sign(
            prediction.Fixture.TeamOneActualGoal.Value - prediction.Fixture.TeamTwoActualGoal.Value);

        return predictedResult == actualResult;
    }
}