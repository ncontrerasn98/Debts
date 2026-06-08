using CreditScore.Api.Data;
using CreditScore.Api.DTOs;
using CreditScore.Api.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace CreditScore.Api.Endpoints;

public static class CreditScoreEndpoints
{
    public static void MapCreditScoreEndpoints(this WebApplication app)
    {
        app.MapGet("/credit-score/{userId:guid}", async (
            Guid userId,
            CreditScoreDbContext dbContext) =>
        {
            var history = await dbContext.CreditHistories
                .FirstOrDefaultAsync(h => h.UserId == userId);

            if (history is null)
                return Results.NotFound(new { Message = "No credit history found for this user" });

            var score = CreditScoreCalculator.Calculate(history);
            var rating = CreditScoreCalculator.GetRating(score);

            return Results.Ok(new CreditScoreResponse(
                UserId: userId,
                Score: score,
                Rating: rating,
                SettledDebts: history.SettledDebts,
                ActiveDebts: history.ActiveDebts,
                UpdatedAt: history.UpdatedAt
            ));
        });
        
        // En CreditScoreEndpoints.cs
        app.MapGet("/credit-score/ranking", async (
            IConnectionMultiplexer redis,
            int top = 10) =>
        {
            var db = redis.GetDatabase();

            var ranking = await db.SortedSetRangeByRankWithScoresAsync(
                "credit-score-ranking",
                0, top - 1,
                Order.Descending);

            var result = ranking.Select((entry, index) => new
            {
                Position = index + 1,
                UserId = entry.Element.ToString(),
                Score = (int)entry.Score
            });

            return Results.Ok(result);
        });
    }
}