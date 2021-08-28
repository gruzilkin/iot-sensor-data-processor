using System;
using System.Text;
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
            this.factory.HostName = "malina";
            this.factory.DispatchConsumersAsync = true;
        }

        public ChannelReader<byte[]> SubscribeAndWrap(String device, CancellationToken ct)
        {
            IConnection conn = factory.CreateConnection();
            IModel channel = conn.CreateModel();

            var pipe = Channel.CreateUnbounded<byte[]>();

            String queueName = "web." + device;
            String routingKey = "temperature." + device;
            channel.QueueDeclare(queueName, false, false, true, null);
            channel.QueueBind(queueName, "amq.topic", routingKey, null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async(ch, ea) =>
                            {
                                var body = ea.Body.ToArray();
                                await pipe.Writer.WriteAsync(body);
                            };
            String consumerTag = channel.BasicConsume(queueName, true, consumer);

            Task.Run(() => {
                WaitHandle.WaitAny(new []{ct.WaitHandle});
                channel.Close();
                conn.Close();
                pipe.Writer.Complete();
                Console.WriteLine("Rabbit connection closed");
            });

            return pipe.Reader;
        }
    }
}