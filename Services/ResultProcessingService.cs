using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Services;

public class ResultProcessingService : IResultProcessingService
{
    private readonly ApplicationDbContext _context;

    public ResultProcessingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResultProcessingResult> ProcessAsync(
        int fixtureId,
        string? processedByUserId,
        string source)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var fixture = await _context.Fixtures
                .Include(x => x.Predictions)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == fixtureId);

            if (fixture == null)
            {
                return new ResultProcessingResult
                {
                    Success = false,
                    FixtureId = fixtureId,
                    ErrorMessage = "Fixture not found."
                };
            }

            if (fixture.IsProcessed)
            {
                return new ResultProcessingResult
                {
                    Success = false,
                    FixtureId = fixtureId,
                    ErrorMessage = "This fixture has already been processed.",
                    RedirectAction = "Index",
                    RedirectController = "Fixtures",
                    RedirectRouteValues = new { area = "Admin" }
                };
            }

            if (fixture.TeamOneActualGoal == null || fixture.TeamTwoActualGoal == null)
            {
                return new ResultProcessingResult
                {
                    Success = false,
                    FixtureId = fixtureId,
                    ErrorMessage = "Please enter the actual match score before processing.",
                    RedirectAction = "Edit",
                    RedirectController = "Fixtures",
                    RedirectRouteValues = new { area = "Admin", id = fixtureId }
                };
            }

            var unprocessedPredictions = fixture.Predictions
                .Where(x => !x.IsProcessed)
                .ToList();

            var exactPredictionCount = 0;
            var correctResultCount = 0;
            var zeroPointCount = 0;

            foreach (var prediction in unprocessedPredictions)
            {
                var point = CalculatePoint(
                    prediction.TeamOnePredictedGoal,
                    prediction.TeamTwoPredictedGoal,
                    fixture.TeamOneActualGoal.Value,
                    fixture.TeamTwoActualGoal.Value
                );

                prediction.EarnedPoint = point;
                prediction.IsProcessed = true;
                prediction.UpdatedAt = DateTime.Now;

                prediction.User.TotalScore += point;

                if (point == 3)
                {
                    prediction.User.ExactPredictionCount += 1;
                    exactPredictionCount++;
                }
                else if (point == 1)
                {
                    correctResultCount++;
                }
                else
                {
                    zeroPointCount++;
                }
            }

            fixture.IsProcessed = true;
            fixture.Status = MatchStatus.Finished;
            fixture.UpdatedAt = DateTime.Now;

            _context.ResultProcessingLogs.Add(new ResultProcessingLog
            {
                FixtureId = fixture.Id,
                ProcessedAt = DateTime.Now,
                ProcessedByUserId = processedByUserId,
                TotalPredictionsProcessed = unprocessedPredictions.Count
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ResultProcessingResult
            {
                Success = true,
                FixtureId = fixtureId,
                TotalPredictionsProcessed = unprocessedPredictions.Count,
                ExactPredictionCount = exactPredictionCount,
                CorrectResultCount = correctResultCount,
                ZeroPointCount = zeroPointCount,
                Message = $"Result processed successfully. {unprocessedPredictions.Count} predictions updated.",
                RedirectAction = "Index",
                RedirectController = "Fixtures",
                RedirectRouteValues = new { area = "Admin" }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return new ResultProcessingResult
            {
                Success = false,
                FixtureId = fixtureId,
                ErrorMessage = ex.Message,
                RedirectAction = "Index",
                RedirectController = "Fixtures",
                RedirectRouteValues = new { area = "Admin" }
            };
        }
    }

    public async Task<ResultProcessingResult> UndoAsync(
        int fixtureId,
        string? processedByUserId,
        string source)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var fixture = await _context.Fixtures
                .Include(x => x.Predictions)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == fixtureId);

            if (fixture == null)
            {
                return new ResultProcessingResult
                {
                    Success = false,
                    FixtureId = fixtureId,
                    ErrorMessage = "Fixture not found."
                };
            }

            if (!fixture.IsProcessed)
            {
                return new ResultProcessingResult
                {
                    Success = false,
                    FixtureId = fixtureId,
                    ErrorMessage = "This fixture is not processed yet.",
                    RedirectAction = "Index",
                    RedirectController = "Fixtures",
                    RedirectRouteValues = new { area = "Admin" }
                };
            }

            var processedPredictions = fixture.Predictions
                .Where(x => x.IsProcessed)
                .ToList();

            foreach (var prediction in processedPredictions)
            {
                var previousPoint = prediction.EarnedPoint;

                prediction.User.TotalScore = Math.Max(
                    0,
                    prediction.User.TotalScore - previousPoint
                );

                if (previousPoint == 3)
                {
                    prediction.User.ExactPredictionCount = Math.Max(
                        0,
                        prediction.User.ExactPredictionCount - 1
                    );
                }

                prediction.EarnedPoint = 0;
                prediction.IsProcessed = false;
                prediction.UpdatedAt = DateTime.Now;
            }

            var logs = await _context.ResultProcessingLogs
                .Where(x => x.FixtureId == fixture.Id)
                .ToListAsync();

            _context.ResultProcessingLogs.RemoveRange(logs);

            fixture.IsProcessed = false;
            fixture.Status = MatchStatus.Finished;
            fixture.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ResultProcessingResult
            {
                Success = true,
                FixtureId = fixtureId,
                TotalPredictionsProcessed = processedPredictions.Count,
                Message = $"Processing undone successfully. {processedPredictions.Count} prediction score(s) were reverted. You can now edit the actual goals and process again.",
                RedirectAction = "Edit",
                RedirectController = "Fixtures",
                RedirectRouteValues = new { area = "Admin", id = fixtureId }
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return new ResultProcessingResult
            {
                Success = false,
                FixtureId = fixtureId,
                ErrorMessage = ex.Message,
                RedirectAction = "Index",
                RedirectController = "Fixtures",
                RedirectRouteValues = new { area = "Admin" }
            };
        }
    }

    private static int CalculatePoint(
        int predictedOne,
        int predictedTwo,
        int actualOne,
        int actualTwo)
    {
        if (predictedOne == actualOne && predictedTwo == actualTwo)
        {
            return 3;
        }

        var predictedResult = predictedOne.CompareTo(predictedTwo);
        var actualResult = actualOne.CompareTo(actualTwo);

        return predictedResult == actualResult ? 1 : 0;
    }
}