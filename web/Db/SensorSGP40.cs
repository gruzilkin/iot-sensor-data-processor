using System;
using System.Collections.Generic;

namespace web.Db
{
    public partial class SensorSGP40
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public decimal Voc { get; set; }
        public DateTime ReceivedAt { get; set; }
        public bool Render { get; set; }
    }
}
