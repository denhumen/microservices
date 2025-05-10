using Confluent.Kafka;
using MessagesService.Services;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var brokers = builder.Configuration["BootstrapServers"]
               ?? throw new InvalidOperationException("Missing BootstrapServers");

builder.Services.AddSingleton(new ConsumerConfig
 {
    BootstrapServers = brokers,
    GroupId = "messages-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
});

builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();

var messagingServiceUrl = builder.Configuration["MessagingServiceUrl"] ?? "http://0.0.0.0:5002";
var configServerUrl = builder.Configuration["ConfigServerUrl"] ?? "http://0.0.0.0:5001";

using (var client = new HttpClient())
{
    var registration = new { ServiceName = "MessagesService", Url = messagingServiceUrl };
    try
    {
        var result = await client.PostAsJsonAsync($"{configServerUrl}/api/config/register", registration);
        if (result.IsSuccessStatusCode)
        {
            Console.WriteLine($"[MessagesService] Registered at {messagingServiceUrl} with ConfigServer.");
        }
        else
        {
            Console.WriteLine($"[MessagesService] Registration failed with status: {result.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MessagesService] Exception during registration: {ex.Message}");
    }
}

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    using (var client = new HttpClient())
    {
        var registration = new { ServiceName = "MessagesService", Url = messagingServiceUrl };
        try
        {
            client.PostAsJsonAsync($"{configServerUrl}/api/config/unregister", registration).Wait();
            Console.WriteLine($"[MessagesService] Unregistered {messagingServiceUrl} from ConfigServer.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MessagesService] Exception during unregistration: {ex.Message}");
        }
    }
});


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