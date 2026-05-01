using ApplicationInfra.Messaging.Abstractions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInfra.Messaging.Kafka.MassTransit;

internal sealed class MassTransitConsumerBridge<TEvent> : IConsumer<TEvent>
    where TEvent : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _name;

    public MassTransitConsumerBridge(
        IServiceProvider serviceProvider,
        string name)
    {
        _serviceProvider = serviceProvider;
        _name = name;
    }

    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var processor = _serviceProvider.GetRequiredKeyedService<IEventProcessor<TEvent>>(_name);
        var eventContext = BuildEventContext(context);

        await processor
            .ProcessEventAsync(context.Message, eventContext, context.CancellationToken)
            .ConfigureAwait(false);
    }

    private static EventContext BuildEventContext(ConsumeContext<TEvent> context)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Headers.GetAll())
        {
            if (header.Value is string stringValue)
            {
                headers[header.Key] = stringValue;
            }
        }

        // MassTransit's ConsumeContext<T> does not expose Kafka partition/offset directly.
        var attributes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        return new EventContext(
            context.MessageId?.ToString(),
            headers,
            attributes);
    }
}
