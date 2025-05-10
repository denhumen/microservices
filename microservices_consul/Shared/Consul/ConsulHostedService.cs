using System;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Consul
{
    public class ConsulHostedService : IHostedService
    {
        private readonly IConsulClient _consul;
        private readonly ILogger<ConsulHostedService> _logger;
        private readonly string _serviceId;
        private readonly AgentServiceRegistration _registration;

        public ConsulHostedService(IConsulClient consul,
                               IConfiguration config,
                               IHostApplicationLifetime lifetime,
                               ILogger<ConsulHostedService> logger)
        {
            _consul = consul;
            _logger = logger;

            var serviceName = config["Consul:ServiceName"];
            var serviceHost = config["Consul:ServiceHost"];
            var servicePort = int.Parse(config["Consul:ServicePort"]!);

            _serviceId = $"{serviceName}-{Guid.NewGuid()}";
            _registration = new AgentServiceRegistration
            {
                ID = _serviceId,
                Name = serviceName,
                Address = serviceHost,
                Port = servicePort,
                Check = new AgentServiceCheck
                {
                    HTTP = $"http://{serviceHost}:{servicePort}/health",
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(5),
                    DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
                }
            };

            lifetime.ApplicationStopping.Register(() =>
            {
                _logger.LogInformation("Deregistering service {ServiceId}", _serviceId);
                _consul.Agent.ServiceDeregister(_serviceId).Wait();
            });
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registering {ServiceId}", _registration.ID);
            await _consul.Agent.ServiceRegister(_registration, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
