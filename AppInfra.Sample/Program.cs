using AppInfra.Kafka.Extensions;
using AppInfra.Sample;
using AppInfra.Serialization.Extensions;
using AppInfra.Serialization.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsonEventSerialization();

builder.Services.AddKafkaConsumer<OrderPlacedEvent, OrderPlacedConsumerProcessor, JsonEventDeserializer>(
    builder.Configuration, "Orders");

var app = builder.Build();

await app.RunAsync();
