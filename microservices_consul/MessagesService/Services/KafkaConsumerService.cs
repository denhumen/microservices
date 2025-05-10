using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace MessagesService.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConsumer<Null, string> _consumer;
        private static readonly List<string> Messages = new();

        public KafkaConsumerService(ConsumerConfig config)
        {
            _consumer = new ConsumerBuilder<Null, string>(config).Build();
            _consumer.Subscribe("messages");
        }

        protected override Task ExecuteAsync(CancellationToken ct) => Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(ct);
                    Messages.Add(result.Message.Value);
                    Console.WriteLine($"[MessagesSvc] Consumed: {result.Message.Value}");
                }
                catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    Console.WriteLine("[MessagesSvc] Topic not yet available, retrying in 1s...");
                    await Task.Delay(1000, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MessagesSvc] Consumer error: {ex.Message}. Retrying in 1s...");
                    await Task.Delay(1000, ct);
                }
            }
        }, ct);


        public static IReadOnlyList<string> GetAll() => Messages;
    }
}
