using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using web.Db;
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

                var recentData = db.SensorData.OrderByDescending(e => e.ReceivedAt).Take(100).ToList();
                foreach(var row in recentData) 
                {
                    var dto = JsonSerializer.Serialize(new {temperature = row.Temperature, humidity = row.Humidity, ppm = row.Ppm, receivedAt = row.ReceivedAt});
                    var body = Encoding.UTF8.GetBytes(dto);
                    await webSocket.SendAsync(new ArraySegment<byte>(body, 0, body.Length), WebSocketMessageType.Text, true, ct);
                }

                var reader = rabbit.SubscribeAndWrap(device, ct);

                try {
                    await foreach (var body in reader.ReadAllAsync(ct))
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(body, 0, body.Length), WebSocketMessageType.Text, true, ct);
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
