using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;
using web.Db;

namespace web.Dto
{
    public class SensorDataPacket {
        public decimal Temperature {get; private set;}
        public decimal Humidity {get; private set;}
        private decimal Ppm;
        public DateTime ReceivedAt {get; private set;}

        protected  SensorDataPacket() { }

        private static SensorDataPacket round(SensorDataPacket packet)
        {
            packet.Temperature = decimal.Round(packet.Temperature, 1);
            packet.Humidity = decimal.Round(packet.Humidity, 1);
            packet.Ppm = decimal.Round(packet.Ppm);
            return packet;
        }

        public ArraySegment<byte> toBytes() {
            var dto = JsonSerializer.Serialize(this);
            var body = Encoding.UTF8.GetBytes(dto);
            return new ArraySegment<byte>(body, 0, body.Length);
        }

        public static SensorDataPacket fromDb(SensorData data) {
            var packet = new SensorDataPacket();
            packet.Temperature = data.Temperature;
            packet.Humidity = data.Humidity;
            packet.Ppm = data.Ppm;
            packet.ReceivedAt = data.ReceivedAt;

            return round(packet);
        }

        public static SensorDataPacket fromRabbit(BasicDeliverEventArgs ea) {
            var body = ea.Body.ToArray();
            var parsedBody = JsonSerializer.Deserialize<Dictionary<String, Object>>(body);
            var packet = new SensorDataPacket();
            packet.Temperature = decimal.Parse(parsedBody["temperature"].ToString());
            packet.Humidity = decimal.Parse(parsedBody["humidity"].ToString());
            packet.Ppm = decimal.Parse(parsedBody["ppm"].ToString());
            
            packet.ReceivedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)ea.BasicProperties.Headers["timestamp_in_ms"]).DateTime;

            return round(packet);
        }
    }
}