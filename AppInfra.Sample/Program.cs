using AppInfra.Kafka;
using AppInfra.Sample;
using AppInfra.Serialization;
using AppInfra.Serialization.Extensions;
using AppInfra.Serialization.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsonEventSerialization();

builder.Services.AddKafkaConsumer<OrderPlacedEvent, OrderPlacedConsumerProcessor, JsonEventDeserializer>(
    builder.Configuration, "Orders");

builder.Services.AddKafkaConsumer<DashboardPingEvent, DashboardPingConsumerProcessor, JsonEventDeserializer>(
    builder.Configuration, "Dashboard");

var app = builder.Build();

await app.RunAsync();
