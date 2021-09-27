using System;
using System.Collections.Generic;

namespace web.Db
{
    public partial class SensorData
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public decimal Temperature { get; set; }
        public decimal Humidity { get; set; }
        public decimal Ppm { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
