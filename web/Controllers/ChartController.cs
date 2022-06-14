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

        private List<SensorReading> readDb(string sensor, string signal, string device, double percentile, double start=0, double end=1)
        {
            var sql = 
                @$"
                WITH frame AS (
                    SELECT MIN(received_at) + (MAX(received_at)-MIN(received_at))*{{2}} as start,
                    MIN(received_at) + (MAX(received_at)-MIN(received_at))*{{3}} as end
                    FROM sensor_data_scd30
                ),
                top_weights AS (
					SELECT id, {signal} as signal, received_at, PERCENT_RANK() OVER (ORDER BY weight DESC) percentile
					FROM sensor_data_{sensor}
					JOIN weights_{sensor}_{signal} USING (id)
					WHERE device_id = {{0}}
                    AND received_at > (SELECT frame.start FROM frame)
                    AND received_at < (SELECT frame.end FROM frame)
				)
                SELECT combined.*
                FROM (
                    (SELECT {signal} as value, received_at
                    FROM sensor_data_{sensor}
                    WHERE id = (SELECT MIN(id) FROM sensor_data_{sensor} WHERE device_id = {{0}})
                    AND received_at > (SELECT frame.start FROM frame)
                    AND received_at < (SELECT frame.end FROM frame) )
                    UNION
                    (SELECT signal as value, received_at
                    FROM top_weights
                    WHERE percentile < {{1}} )
                    UNION
                    (SELECT {signal} as value, received_at
                    FROM sensor_data_{sensor}
                    WHERE device_id = {{0}} AND id > (SELECT MAX(id) FROM top_weights)
                    AND received_at > (SELECT frame.start FROM frame)
                    AND received_at < (SELECT frame.end FROM frame) )
                ) combined
                ORDER BY received_at ASC";

            var readings = db.SensorReading.FromSqlRaw(sql, device, percentile, start, end).ToList();
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

        public JsonResult Json(string id, double start, double end)
        {
            var model = new ChartModel() { Device = id };
            var percentile = 0.1 / (end - start);
            model.Temperature = readDb("scd30", "temperature", id, percentile, start, end).Select(r => SensorDataPacket.forTemperature(r)).ToList();
            model.Humidity = readDb("scd30", "humidity", id, percentile, start, end).Select(r => SensorDataPacket.forHumidity(r)).ToList();
            model.Ppm = readDb("scd30", "ppm", id, percentile, start, end).Select(r => SensorDataPacket.forPpm(r)).ToList();
            model.Voc = readDb("sgp40", "voc", id, percentile, start, end).Select(r => SensorDataPacket.forVoc(r)).ToList();

            return Json(model);
        } 
    }
}