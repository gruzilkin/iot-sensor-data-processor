using System;
using System.Collections.Generic;

namespace web.Db
{
    public partial class SensorCalibrationData
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public decimal Temperature { get; set; }
        public decimal Humidity { get; set; }
        public decimal R0 { get; set; }
        public decimal Ppm { get; set; }
        public DateTime ReceivedAt { get; set; }
        public bool? IsOutlier { get; set; }
        public TimeSpan Uptime { get; set; }
        public bool? IsInvalid { get; set; }
    }
}
