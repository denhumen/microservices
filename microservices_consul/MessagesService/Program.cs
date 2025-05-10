using Confluent.Kafka;
using Consul;
using MessagesService.Services;
using Shared.Consul;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var configServerUrl = builder.Configuration["Consul:Host"]
                      ?? throw new InvalidOperationException("Consul config is not set!");


builder.Services.AddSingleton<IConsulClient>(sp =>
  new ConsulClient(cfg => {
      cfg.Address = new Uri(configServerUrl!);
  })
);

var tempProvider = builder.Services.BuildServiceProvider();
var consul = tempProvider.GetRequiredService<IConsulClient>();
var kv = consul.KV;

var bsPair = kv.Get("config/messaging/BootstrapServers").GetAwaiter().GetResult();
var brokers = Encoding.UTF8.GetString(bsPair.Response.Value);

var tpPair = kv.Get("config/messaging/Topic").GetAwaiter().GetResult();
var topic = Encoding.UTF8.GetString(tpPair.Response.Value);

Console.WriteLine($"[MessagesService] Loaded brokers={brokers}, topic={topic} from Consul KV");

builder.Services.AddSingleton(new ConsumerConfig
 {
    BootstrapServers = brokers,
    GroupId = "messages-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
});

builder.Services.AddHostedService<ConsulHostedService>();

builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok());

var messagingServiceUrl = builder.Configuration["MessagingServiceUrl"] ?? "http://0.0.0.0:5002";

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run(messagingServiceUrl);