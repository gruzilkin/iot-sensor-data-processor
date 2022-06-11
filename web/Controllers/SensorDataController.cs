using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using web.Db;
using web.Dto;
using web.Services;

namespace web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SensorDataController : ControllerBase
    {
        private readonly ILogger<SensorDataController> _logger;

        private readonly RabbitMQService rabbit;

        private readonly PostgresContext db;

        public SensorDataController(ILogger<SensorDataController> logger, RabbitMQService rabbit, PostgresContext db)
        {
            _logger = logger;
            this.rabbit = rabbit;
            this.db = db;
        }

        private List<SensorReading> readDb(String sensor, String signal, String device, double percentile) {
            var sql = @$"WITH top_weights AS (
					SELECT id, {signal} as signal, received_at, PERCENT_RANK() OVER (ORDER BY weight DESC) percentile
					FROM sensor_data_{sensor}
					JOIN weights_{sensor}_{signal} USING (id)
					WHERE device_id = {{0}}
				)
				(SELECT {signal} as value, received_at
				FROM sensor_data_{sensor}
				WHERE device_id = {{0}}
				ORDER BY id ASC LIMIT 1)
				UNION
				(SELECT signal as value, received_at
				FROM top_weights
				WHERE percentile < {{1}}
                ORDER BY received_at DESC)
				UNION
				(SELECT {signal} as value, received_at
				FROM sensor_data_{sensor}
				WHERE device_id = {{0}}
				ORDER BY id DESC LIMIT 1)";
            var readings = db.SensorReading.FromSqlRaw(sql, device, percentile)
            .ToList();
            return readings;
        }

        [HttpGet("{device}")]
        public async Task Get(string device)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                CancellationToken ct = HttpContext.RequestAborted;
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var config = new Dictionary<string, List<string>>()
                {
                    { "scd30", new List<string>() {"temperature", "humidity", "ppm"} },
                    {"sgp40",new List<string>() {"voc"}}
                };

                var setters = new Dictionary<String, Func<SensorReading, SensorDataPacket>>()
                {
                    {"temperature", (SensorReading p) => SensorDataPacket.forTemperature(p)},
                    {"humidity", (SensorReading p) => SensorDataPacket.forHumidity(p)},
                    {"ppm", (SensorReading p) => SensorDataPacket.forPpm(p)},
                    {"voc", (SensorReading p) => SensorDataPacket.forVoc(p)}
                };

                foreach( var (sensor, signals) in config) {
                    foreach(var signal in signals) {
                        var readings = readDb(sensor, signal, device, 0.1);
                        foreach(var reading in readings) 
                        {
                            var packet =setters[signal](reading);
                            await webSocket.SendAsync(packet.toBytes(), WebSocketMessageType.Text, true, ct);
                        }
                    }
                }

                var reader = rabbit.SubscribeAndWrap(device, ct);

                try {
                    await foreach (var packet in reader.ReadAllAsync(ct))
                    {
                        await webSocket.SendAsync(packet.toBytes(), WebSocketMessageType.Text, true, ct);
                    }
                }
                catch (OperationCanceledException) {}
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
