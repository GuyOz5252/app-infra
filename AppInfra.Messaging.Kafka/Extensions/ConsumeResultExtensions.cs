using System.Globalization;
using System.Text;
using AppInfra.Messaging.Abstractions;
using Confluent.Kafka;

namespace AppInfra.Messaging.Kafka.Extensions;

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

        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [KafkaEventContextAttributes.Partition] = result.Partition.Value.ToString(CultureInfo.InvariantCulture),
            [KafkaEventContextAttributes.Offset] = result.Offset.Value.ToString(CultureInfo.InvariantCulture),
        };

        return new EventContext(
            result.Message.Key,
            headers,
            attributes);
    }
}
