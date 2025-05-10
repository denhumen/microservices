using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LoggingService.Services;
using Consul;
using Shared.Consul;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Starting LoggingService...");

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();


var configServerUrl = builder.Configuration["Consul:Host"]
                      ?? throw new InvalidOperationException("Consul config is not set!");

builder.Services.AddSingleton<IConsulClient>(sp =>
  new ConsulClient(cfg =>
  {
      cfg.Address = new Uri(configServerUrl!);
  })
);

builder.Services.AddHostedService<ConsulHostedService>();
builder.Services.AddSingleton<HazelcastService>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok());

var hazelcastService = app.Services.GetRequiredService<HazelcastService>();
await hazelcastService.InitializeAsync();

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
