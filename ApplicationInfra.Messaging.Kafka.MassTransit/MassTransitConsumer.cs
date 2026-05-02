using ApplicationInfra.Messaging.Abstractions;
using ApplicationInfra.Messaging.Kafka.MassTransit.Extensions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationInfra.Messaging.Kafka.MassTransit;

internal sealed class MassTransitConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _name;

    public MassTransitConsumer(
        IServiceProvider serviceProvider,
        string name)
    {
        _serviceProvider = serviceProvider;
        _name = name;
    }

    public async Task Consume(ConsumeContext<TEvent> context)
    {
        var processor = _serviceProvider.GetRequiredKeyedService<IEventProcessor<TEvent>>(_name);

        await processor.ProcessEventAsync(context.Message, context.ToEventContext(), context.CancellationToken)
            .ConfigureAwait(false);
    }
}
