using System;
using System.Collections.Generic;

namespace web.Db
{
    public partial class CalibrationModels
    {
        public string DeviceId { get; set; }
        public byte[] Model { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
