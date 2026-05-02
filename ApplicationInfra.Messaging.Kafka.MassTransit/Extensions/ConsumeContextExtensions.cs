using ApplicationInfra.Messaging.Abstractions;
using MassTransit;
using MassTransit.KafkaIntegration;

namespace ApplicationInfra.Messaging.Kafka.MassTransit.Extensions;

internal static class ConsumeContextExtensions
{
    internal static EventContext ToEventContext<T>(this ConsumeContext<T> consumeContext) where T : class
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in consumeContext.Headers.GetAll())
        {
            headers[header.Key] = header.Value.ToString()!;
        }

        if (!consumeContext.TryGetPayload<KafkaConsumeContext<string, T>>(out var kafkaContext))
        {
            return new EventContext(
                consumeContext.PartitionKey(),
                headers,
                new Dictionary<string, string?>());
        }

        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [KafkaEventContextAttributes.Partition] = kafkaContext.PartitionKey(),
            // TODO: Add offset
        };
            
        return new EventContext(
            kafkaContext.PartitionKey(),
            headers,
            attributes);
    }
}
