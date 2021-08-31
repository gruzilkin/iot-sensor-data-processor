using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace web.Services
{
    public class RabbitMQService
    {
        private readonly ILogger<RabbitMQService> logger;

        private readonly ConnectionFactory factory;

        public RabbitMQService(ILogger<RabbitMQService> logger)
        {
            this.logger = logger;

            this.factory = new ConnectionFactory();
            this.factory.HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
            this.factory.DispatchConsumersAsync = true;
        }

        public ChannelReader<byte[]> SubscribeAndWrap(String device, CancellationToken ct)
        {
            IConnection conn = factory.CreateConnection();
            IModel channel = conn.CreateModel();

            var pipe = Channel.CreateUnbounded<byte[]>();

            String readerQueue = $"sensor.live.data.{device}.web";
            channel.QueueDeclare(readerQueue, false, false, true, null);
            channel.QueueBind(readerQueue, "amq.topic", $"sensor.live.data.{device}", null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async(ch, ea) =>
                            {
                                var body = ea.Body.ToArray();
                                await pipe.Writer.WriteAsync(body);
                            };
            String consumerTag = channel.BasicConsume(readerQueue, true, consumer);

            Task.Run(() => {
                WaitHandle.WaitAny(new []{ct.WaitHandle});
                channel.Close();
                conn.Close();
                pipe.Writer.Complete();
                logger.LogInformation("rabbit connection closed");
            });

            return pipe.Reader;
        }
    }
}
