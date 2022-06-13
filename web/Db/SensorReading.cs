using System;
using System.Collections.Generic;

namespace web.Db
{
    public partial class SensorReading
    {
        public decimal Value { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
