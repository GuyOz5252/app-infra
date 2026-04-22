using AppInfra.Serialization.Json;
using AppInfra.Serialization.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppInfra.Serialization.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AddJsonEventSerialization()
        {
            services.TryAddSingleton<JsonEventDeserializer>();
            services.TryAddSingleton<JsonEventSerializer>();
        }

        public void AddProtobufEventSerialization(Action<ProtobufMessageParserResolverBuilder> configureProtobufParsers)
        {
            var builder = new ProtobufMessageParserResolverBuilder();
            configureProtobufParsers(builder);
            services.TryAddSingleton<ProtobufMessageParserResolver>(_ => builder.Build());
            services.TryAddSingleton<ProtobufEventDeserializer>();
            services.TryAddSingleton<ProtobufEventSerializer>();
        }
    }
}
