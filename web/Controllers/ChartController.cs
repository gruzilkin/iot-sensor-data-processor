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

        private List<SensorReading> readDb(string sensor, string signal, string device, double percentile)
        {
            var sql = 
                @$"WITH top_weights AS (
					SELECT id, {signal} as signal, received_at, PERCENT_RANK() OVER (ORDER BY weight DESC) percentile
					FROM sensor_data_{sensor}
					JOIN weights_{sensor}_{signal} USING (id)
					WHERE device_id = {{0}}
				)
                SELECT combined.*
                FROM (
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
                    ORDER BY id DESC LIMIT 1)
                ) combined
                ORDER BY received_at ASC";

            var readings = db.SensorReading.FromSqlRaw(sql, device, percentile).ToList();
            return readings;
        }

        public IActionResult Index(string id)
        {
            var model = new ChartModel() { Device = id };

            model.Temperature = readDb("scd30", "temperature", id, 0.1).Select(r => SensorDataPacket.forTemperature(r)).ToList();
            model.Humidity = readDb("scd30", "humidity", id, 0.1).Select(r => SensorDataPacket.forHumidity(r)).ToList();
            model.Ppm = readDb("scd30", "ppm", id, 0.1).Select(r => SensorDataPacket.forPpm(r)).ToList();
            model.Voc = readDb("sgp40", "voc", id, 0.1).Select(r => SensorDataPacket.forVoc(r)).ToList();
            
            return View(model);
        }
    }
}