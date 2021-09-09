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
        
        private decimal? ppm;
        public decimal? Ppm {
            get { return ppm; }
            private set { ppm = value.HasValue ? Math.Round(value.Value): value;}
        }
        public DateTime ReceivedAt {get; private set;}

        protected  SensorDataPacket() { }

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

            return packet;
        }

        public static SensorDataPacket fromRabbit(BasicDeliverEventArgs ea) {
            var body = ea.Body.ToArray();
            var parsedBody = JsonSerializer.Deserialize<Dictionary<String, String>>(body);
            var packet = new SensorDataPacket();
            packet.Temperature = Decimal.Parse(parsedBody["Temperature"]);
            packet.Humidity = Decimal.Parse(parsedBody["Humidity"]);
            if (parsedBody.ContainsKey("Ppm")) {
                packet.Ppm = Decimal.Parse(parsedBody["Ppm"]);
            }
            packet.ReceivedAt = DateTime.Parse(ea.BasicProperties.Headers["timestamp_in_ms"].ToString());

            return packet;
        }
    }
}