using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using web.Dto;

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
            this.factory.UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER");
            this.factory.Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS");
            this.factory.DispatchConsumersAsync = true;
        }

        public ChannelReader<SensorDataPacket> SubscribeAndWrap(String device, CancellationToken ct)
        {
            IConnection conn = factory.CreateConnection();
            IModel channel = conn.CreateModel();

            var pipe = Channel.CreateUnbounded<SensorDataPacket>();

            String readerQueue = $"sensor.data.{device}.web";
            channel.QueueDeclare(readerQueue, false, false, true, null);
            channel.QueueBind(readerQueue, "amq.topic", $"sensor.data.{device}", null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async(ch, ea) =>
                            {
                                var packet = SensorDataPacket.fromRabbit(ea);
                                await pipe.Writer.WriteAsync(packet);
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
