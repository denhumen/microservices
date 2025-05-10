using Polly;
using Confluent.Kafka;
using FacadeService.Services;
using Consul;
using Shared.Consul;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var configServerUrl = builder.Configuration["Consul:Host"]
                      ?? throw new InvalidOperationException("Consul config is not set!");

builder.Services.AddSingleton<IConsulClient>(sp =>
  new ConsulClient(cfg => {
      cfg.Address = new Uri(configServerUrl!);
  })
);

var sp = builder.Services.BuildServiceProvider();
var consul = sp.GetRequiredService<IConsulClient>();
var kv = consul.KV;

var bsPair = await kv.Get("config/messaging/BootstrapServers");
var brokers = Encoding.UTF8.GetString(bsPair.Response.Value);
Console.WriteLine($"[MessagesService] Brokers={brokers}");

builder.Services.AddSingleton(new ProducerConfig
{
    BootstrapServers = brokers
});
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
  new ProducerBuilder<Null, string>(sp.GetRequiredService<ProducerConfig>())
    .Build()
);

builder.Services.AddHostedService<ConsulHostedService>();
builder.Services.AddSingleton<ConsulServiceDiscovery>();

var retryPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => !r.IsSuccessStatusCode)
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine($"[FacadeService] Retry attempt {retryAttempt} due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
        });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("LoggingServiceClient").AddPolicyHandler(retryPolicy);

var app = builder.Build();
app.MapGet("/health", () => Results.Ok());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
