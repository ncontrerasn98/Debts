using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Debts.Infrastructure.HealthChecks;

public class KafkaHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;

    public KafkaHealthCheck(IConfiguration configuration)
        => _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(
                    new AdminClientConfig { BootstrapServers = _bootstrapServers })
                .Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));

            return metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy($"{metadata.Brokers.Count} broker(s) available")
                : HealthCheckResult.Unhealthy("No brokers available");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}