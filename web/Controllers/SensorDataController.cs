﻿using System;
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

        [HttpGet("{device}")]
        public async Task Get(string device)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                CancellationToken ct = HttpContext.RequestAborted;
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                var recentData = db.SensorData.FromSqlInterpolated(@$"
                    SELECT AVG(temperature) as temperature, AVG(humidity) as humidity, AVG(ppm) as ppm, date_trunc('minute', received_at) as received_at
                    FROM sensor_data
                    WHERE received_at > now() - interval '1 day' AND device_id = {device}
                    GROUP BY device_id, date_trunc('minute', received_at)
                    ORDER BY received_at")
                .Select(e => new {Humidity = e.Humidity, Temperature = e.Temperature, Ppm = e.Ppm, ReceivedAt = e.ReceivedAt}).ToList();

                foreach(var row in recentData) 
                {
                    var packet = SensorDataPacket.fromRaw(row.Temperature, row.Humidity, row.Ppm, row.ReceivedAt);
                    await webSocket.SendAsync(packet.toBytes(), WebSocketMessageType.Text, true, ct);
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
