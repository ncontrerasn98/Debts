using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Debts.Application.Abstractions.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Debts.Infrastructure.Persistence.Messaging.Producer;

public class KafkaProducer : IEventProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    
    private static readonly ActivitySource _activitySource = new("KafkaProducer");
    private static readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers =
                configuration["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message)
    {
        using var activity = _activitySource.StartActivity(
            $"kafka.produce {topic}",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.destination_kind", "topic");
        
        var kafkaMessage = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = JsonSerializer.Serialize(message),
            Headers = new Headers()
        };
        
        _propagator.Inject(
            new PropagationContext(activity?.Context ?? default, Baggage.Current),
            kafkaMessage.Headers,
            (headers, key, value) =>
                headers.Add(key, Encoding.UTF8.GetBytes(value)));

        await _producer.ProduceAsync(topic, kafkaMessage);
        _producer.Flush(TimeSpan.FromSeconds(5));

        _logger.LogInformation("Publishing message to topic {Topic}", topic);
    }
    
}