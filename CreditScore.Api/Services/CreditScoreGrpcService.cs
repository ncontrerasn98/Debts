using CreditScore.Api.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace CreditScore.Api.Services;

public class CreditScoreGrpcService : CreditScoreService.CreditScoreServiceBase
{
    private readonly CreditScoreDbContext _dbContext;
    private readonly ILogger<CreditScoreGrpcService> _logger;

    public CreditScoreGrpcService(
        CreditScoreDbContext dbContext,
        ILogger<CreditScoreGrpcService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task<GetScoreResponse> GetScore(
        GetScoreRequest request,
        ServerCallContext context)
    {
        var userId = Guid.Parse(request.UserId);

        var history = await _dbContext.CreditHistories
            .FirstOrDefaultAsync(h => h.UserId == userId);

        if (history is null)
        {
            return new GetScoreResponse
            {
                UserId = request.UserId,
                Score = 0,
                Rating = "Sin historial",
                SettledDebts = 0,
                ActiveDebts = 0,
                UpdatedAt = DateTime.UtcNow.ToString("O")
            };
        }

        var score = CreditScoreCalculator.Calculate(history);
        var rating = CreditScoreCalculator.GetRating(score);

        _logger.LogInformation(
            "gRPC GetScore for user {UserId} — score {Score}",
            userId, score);

        return new GetScoreResponse
        {
            UserId = request.UserId,
            Score = score,
            Rating = rating,
            SettledDebts = history.SettledDebts,
            ActiveDebts = history.ActiveDebts,
            UpdatedAt = history.UpdatedAt.ToString("O")
        };
    }
}