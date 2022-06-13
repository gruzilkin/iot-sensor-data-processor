using System.Collections.Generic;
using web.Dto;

namespace web.Models
{
    public class ChartModel {
        public string Device {get;set;}

        public List<SensorDataPacket> Temperature {get; set;}

        public List<SensorDataPacket> Humidity {get; set;}

        public List<SensorDataPacket> Ppm {get; set;}

        public List<SensorDataPacket> Voc {get; set;}
    }
}