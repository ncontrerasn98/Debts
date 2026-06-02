using Debts.Application.Abstractions.CreditScore;
using Debts.Domain.Exceptions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Shared.Contracts;
using StatusCode = Grpc.Core.StatusCode;

namespace Debts.Infrastructure.CreditScore;

public class CreditScoreGrpcClient : ICreditScoreService
{
    private readonly Shared.Contracts.CreditScoreService.CreditScoreServiceClient _client;
    private readonly ILogger<CreditScoreGrpcClient> _logger;

    public CreditScoreGrpcClient(
        Shared.Contracts.CreditScoreService.CreditScoreServiceClient client,
        ILogger<CreditScoreGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CreditScoreResult?> GetScoreAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetScoreAsync(
                new GetScoreRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);

            return new CreditScoreResult(
                UserId: Guid.Parse(response.UserId),
                Score: response.Score,
                Rating: response.Rating,
                SettledDebts: response.SettledDebts,
                ActiveDebts: response.ActiveDebts,
                UpdatedAt: DateTime.Parse(response.UpdatedAt));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogWarning(ex, "CreditScore gRPC service unavailable");
            throw new ServiceUnavailableException("Credit score service is currently unavailable");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }
}