using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LoggingService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSingleton<HazelcastService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

var loggingServiceUrl = builder.Configuration["LoggingServiceUrl"] ?? "http://localhost:5003";
var configServerUrl = builder.Configuration["ConfigServerUrl"] ?? "http://localhost:5001";

var hazelcastService = app.Services.GetRequiredService<HazelcastService>();
await hazelcastService.InitializeAsync();

using (var client = new HttpClient())
{
    var registration = new { ServiceName = "LoggingService", Url = loggingServiceUrl };
    try
    {
        var result = await client.PostAsJsonAsync($"{configServerUrl}/api/config/register", registration);
        if (result.IsSuccessStatusCode)
        {
            Console.WriteLine($"[LoggingService] Registered at {loggingServiceUrl} with ConfigServer.");
        }
        else
        {
            Console.WriteLine($"[LoggingService] Registration failed with status: {result.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LoggingService] Exception during registration: {ex.Message}");
    }
}

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    using (var client = new HttpClient())
    {
        var registration = new { ServiceName = "LoggingService", Url = loggingServiceUrl };
        try
        {
            client.PostAsJsonAsync($"{configServerUrl}/api/config/unregister", registration).Wait();
            Console.WriteLine($"[LoggingService] Unregistered {loggingServiceUrl} from ConfigServer.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoggingService] Exception during unregistration: {ex.Message}");
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
app.Run(loggingServiceUrl);
