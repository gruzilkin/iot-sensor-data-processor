﻿using System;
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

        public SensorDataController(ILogger<SensorDataController> logger, RabbitMQService rabbit)
        {
            _logger = logger;
            this.rabbit = rabbit;
        }

        [HttpGet("{device}")]
        public async Task Get(string device)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                CancellationToken ct = HttpContext.RequestAborted;
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

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
