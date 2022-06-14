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
        public long ReceivedAt {get; private set;}

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

        private static long ToUnixTimestamp(DateTime dt) 
        {
            return ((DateTimeOffset)dt).ToUnixTimeMilliseconds();
        }

        public ArraySegment<byte> toBytes() {
            var dto = JsonSerializer.Serialize(this, new JsonSerializerOptions { IgnoreNullValues = true });
            var body = Encoding.UTF8.GetBytes(dto);
            return new ArraySegment<byte>(body, 0, body.Length);
        }

        public static SensorDataPacket forTemperature(SensorReading data) {
            var packet = new SensorDataPacket();
            packet.Temperature = data.Value;
            packet.ReceivedAt = ToUnixTimestamp(data.ReceivedAt);
            return round(packet);
        }

        public static SensorDataPacket forHumidity(SensorReading data) {
            var packet = new SensorDataPacket();
            packet.Humidity = data.Value;
            packet.ReceivedAt = ToUnixTimestamp(data.ReceivedAt);

            return round(packet);
        }

        public static SensorDataPacket forPpm(SensorReading data) {
            var packet = new SensorDataPacket();
            packet.Ppm = data.Value;
            packet.ReceivedAt = ToUnixTimestamp(data.ReceivedAt);

            return round(packet);
        }

        public static SensorDataPacket forVoc(SensorReading data) {
            var packet = new SensorDataPacket();
            packet.Voc = data.Value;
            packet.ReceivedAt = ToUnixTimestamp(data.ReceivedAt);

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
            
            packet.ReceivedAt = (long)ea.BasicProperties.Headers["timestamp_in_ms"];

            return round(packet);
        }
    }
}