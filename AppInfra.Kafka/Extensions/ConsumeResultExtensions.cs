using System.Text;
using AppInfra.Messaging.Abstractions;
using Confluent.Kafka;

namespace AppInfra.Kafka.Extensions;

internal static class ConsumeResultExtensions
{
    internal static EventContext ToEventContext(this ConsumeResult<string, byte[]> result)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (result.Message.Headers is not null)
        {
            foreach (var header in result.Message.Headers)
            {
                headers[header.Key] = Encoding.UTF8.GetString(header.GetValueBytes());
            }
        }

        return new EventContext(
            result.Topic,
            result.Message.Key,
            headers,
            result.Partition.Value,
            result.Offset.Value);
    }
}
