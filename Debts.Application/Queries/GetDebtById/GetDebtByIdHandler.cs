using System.Text.Json;
using Debts.Application.Abstractions.Persistence;
using Debts.Application.DTOs;
using Debts.Domain.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Debts.Application.Queries.GetDebtById;

public class GetDebtByIdHandler
{
    private readonly IDebtRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetDebtByIdHandler> _logger;
    

    public GetDebtByIdHandler(IDebtRepository repository, IDistributedCache cache, ILogger<GetDebtByIdHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DebtDto?> Handle(GetDebtByIdQuery query)
    {
        var cacheKey = $"debt:{query.Id}";
        var cachedDebt = await _cache.GetStringAsync(cacheKey);
        
        if (cachedDebt is not null)
        {
            _logger.LogInformation(
                "Debt {DebtId} retrieved from cache",
                query.Id);

            return JsonSerializer.Deserialize<DebtDto>(cachedDebt);
        }
        
        var debt = await _repository.GetByIdAsync(query.Id);

        if (debt is null)
        {
            throw new NotFoundException("Debt not found");
        }
        
        var debtDto = new DebtDto
        {
            Id = debt.Id,
            Amount = debt.OriginalAmount,
            UserId = debt.UserId,
            IsSettled = debt.IsSettled
        };
        
        var serializedDebt = JsonSerializer.Serialize(debtDto);

        await _cache.SetStringAsync(
            cacheKey,
            serializedDebt,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(5)
            });
        
        return debtDto;
    }
}