using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Debts.Infrastructure.Persistence.Messaging;

public class KafkaTopicInitializer : IHostedService
{
    private readonly ILogger<KafkaTopicInitializer> _logger;
    private readonly string _bootstrapServers;

    public KafkaTopicInitializer(
        IConfiguration configuration,
        ILogger<KafkaTopicInitializer> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = _bootstrapServers })
            .Build();

        var topics = new[] { "debt-settled", "debt-settlement-reversed", "credit-score-updated" };

        foreach (var topic in topics)
        {
            try
            {
                await adminClient.CreateTopicsAsync(new[]
                {
                    new TopicSpecification
                    {
                        Name = topic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    }
                });

                _logger.LogInformation("Kafka topic {Topic} created", topic);
            }
            catch (CreateTopicsException ex)
                when (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger.LogInformation("Kafka topic {Topic} already exists", topic);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}