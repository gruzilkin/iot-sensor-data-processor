using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;
using web.Db;

namespace web.Dto
{
    public class SensorDataPacket {
        public decimal? Temperature {get; private set;}
        public decimal? Humidity {get; private set;}
        public decimal? Ppm {get; private set;}

        public decimal? Voc {get; private set;}
        public DateTime ReceivedAt {get; private set;}

        protected  SensorDataPacket() { }

        private static SensorDataPacket round(SensorDataPacket packet)
        {
            if (packet.Temperature.HasValue) {
                packet.Temperature = decimal.Round(packet.Temperature.Value, 2);
            }
            if (packet.Humidity.HasValue) {
                packet.Humidity = decimal.Round(packet.Humidity.Value, 2);
            }
            if (packet.Ppm.HasValue) {
                packet.Ppm = decimal.Round(packet.Ppm.Value);
            }
            if (packet.Voc.HasValue) {
                packet.Voc = decimal.Round(packet.Voc.Value);
            }
            return packet;
        }

        public ArraySegment<byte> toBytes() {
            var dto = JsonSerializer.Serialize(this, new JsonSerializerOptions { IgnoreNullValues = true });
            var body = Encoding.UTF8.GetBytes(dto);
            return new ArraySegment<byte>(body, 0, body.Length);
        }

        public static SensorDataPacket fromDb(SensorSCD30 data) {
            var packet = new SensorDataPacket();
            packet.Temperature = data.Temperature;
            packet.Humidity = data.Humidity;
            packet.Ppm = data.Ppm;
            packet.ReceivedAt = data.ReceivedAt;

            return round(packet);
        }

        public static SensorDataPacket fromDb(SensorSGP40 data) {
            var packet = new SensorDataPacket();
            packet.Voc = data.Voc;
            packet.ReceivedAt = data.ReceivedAt;

            return round(packet);
        }

        public static SensorDataPacket fromRaw(decimal temperature, decimal humidity, decimal ppm, DateTime receivedAt) {
            var packet = new SensorDataPacket();
            packet.Temperature = temperature;
            packet.Humidity = humidity;
            packet.Ppm = ppm;
            packet.ReceivedAt = receivedAt;

            return round(packet);
        }

        public static SensorDataPacket fromRaw(decimal voc, DateTime receivedAt) {
            var packet = new SensorDataPacket();
            packet.Voc = voc;
            packet.ReceivedAt = receivedAt;

            return round(packet);
        }

        public static SensorDataPacket fromRabbit(BasicDeliverEventArgs ea) {
            var body = ea.Body.ToArray();
            var parsedBody = JsonSerializer.Deserialize<Dictionary<String, Object>>(body);
            var packet = new SensorDataPacket();
            if(parsedBody.ContainsKey("temperature")) {
                packet.Temperature = decimal.Parse(parsedBody["temperature"].ToString());
            }
            if(parsedBody.ContainsKey("humidity")) {
                packet.Humidity = decimal.Parse(parsedBody["humidity"].ToString());
            }
            if(parsedBody.ContainsKey("ppm")) {
                packet.Ppm = decimal.Parse(parsedBody["ppm"].ToString());
            }
            if(parsedBody.ContainsKey("voc")) {
                packet.Voc = decimal.Parse(parsedBody["voc"].ToString());
            }
            
            packet.ReceivedAt = DateTimeOffset.FromUnixTimeMilliseconds((long)ea.BasicProperties.Headers["timestamp_in_ms"]).UtcDateTime;

            return round(packet);
        }
    }
}