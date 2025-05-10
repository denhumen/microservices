using Polly;
using Confluent.Kafka;
using FacadeService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var brokers = builder.Configuration["BootstrapServers"]
               ?? throw new InvalidOperationException("Missing BootstrapServers");

builder.Services.AddSingleton(new ProducerConfig
{
    BootstrapServers = brokers
});
builder.Services.AddSingleton<IProducer<Null, string>>(sp =>
  new ProducerBuilder<Null, string>(sp.GetRequiredService<ProducerConfig>())
    .Build()
);

var configServerUrl = builder.Configuration["ConfigServerUrl"]
                      ?? throw new InvalidOperationException("ConfigServerUrl is not set!");

builder.Services
  .AddHttpClient<ServiceDiscovery>(client =>
  {
      client.BaseAddress = new Uri(configServerUrl);
  });

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
