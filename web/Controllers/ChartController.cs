using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using web.Db;
using web.Dto;
using web.Models;

namespace web.Controllers
{
    public class ChartController : Controller
    {
        private readonly ILogger<ChartController> _logger;

        private readonly PostgresContext db;

        public ChartController(ILogger<ChartController> logger, PostgresContext db)
        {
            _logger = logger;
            this.db = db;
        }

        private List<SensorReading> readDb(string sensor, string signal, string device, long limit, DateTime start, DateTime end)
        {
            var sql = 
                @$"
                SELECT combined.*
                FROM (
                    (
                        SELECT {signal} as value, received_at
                        FROM sensor_data_{sensor}
                        WHERE id = ( SELECT MIN(id) FROM sensor_data_{sensor} WHERE device_id = {{0}} AND received_at > {{2}} AND received_at < {{3}} )
                    )
                    UNION
                    (
                        SELECT {signal} as signal, received_at
                        FROM sensor_data_{sensor}
                        JOIN weights_{sensor}_{signal} USING (id)
                        WHERE device_id = {{0}}
                        AND received_at > {{2}}
                        AND received_at < {{3}}
                        ORDER BY weight DESC
                        LIMIT {{1}}
                    )
                    UNION
                    (
                        SELECT {signal} as value, received_at
                        FROM sensor_data_{sensor}
                        WHERE id > ( SELECT MAX(id) FROM sensor_data_{sensor} JOIN weights_{sensor}_{signal} USING (id) WHERE device_id = {{0}} )
                        AND device_id = {{0}}
                        AND received_at > {{2}}
                        AND received_at < {{3}}
                    )
                ) combined
                ORDER BY received_at ASC";

            var readings = db.SensorReading.FromSqlRaw(sql, device, limit, start, end).ToList();
            return readings;
        }

        public IActionResult Index(string id)
        {
            var model = new ChartModel() { Device = id };

            var startTime = DateTime.MinValue;
            var endTime = DateTime.Now;
            
            var limit = int.Parse(Environment.GetEnvironmentVariable("DATA_START_LIMIT"));

            model.Temperature = readDb("scd30", "temperature", id, limit, startTime, endTime).Select(r => SensorDataPacket.forTemperature(r)).ToList();
            model.Humidity = readDb("scd30", "humidity", id, limit, startTime, endTime).Select(r => SensorDataPacket.forHumidity(r)).ToList();
            model.Ppm = readDb("scd30", "ppm", id, limit, startTime, endTime).Select(r => SensorDataPacket.forPpm(r)).ToList();
            model.Voc = readDb("sgp40", "voc", id, limit, startTime, endTime).Select(r => SensorDataPacket.forVoc(r)).ToList();
            
            return View(model);
        }

        public JsonResult Json(string id, long start, long end)
        {
            var model = new ChartModel() { Device = id };
            var startTime = DateTimeOffset.FromUnixTimeMilliseconds(start).UtcDateTime;
            var endTime = DateTimeOffset.FromUnixTimeMilliseconds(end).UtcDateTime;
            
            var limit = int.Parse(Environment.GetEnvironmentVariable("DATA_JSON_LIMIT"));
            
            model.Temperature = readDb("scd30", "temperature", id, limit, startTime, endTime).Select(r => SensorDataPacket.forTemperature(r)).ToList();
            model.Humidity = readDb("scd30", "humidity", id, limit, startTime, endTime).Select(r => SensorDataPacket.forHumidity(r)).ToList();
            model.Ppm = readDb("scd30", "ppm", id, limit, startTime, endTime).Select(r => SensorDataPacket.forPpm(r)).ToList();
            model.Voc = readDb("sgp40", "voc", id, limit, startTime, endTime).Select(r => SensorDataPacket.forVoc(r)).ToList();

            return Json(model);
        } 
    }
}