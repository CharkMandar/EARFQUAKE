using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagisterProject
{
    public class EarthquakeInfo
    {
        public DateTime OriginTime { get; set; }     // время события
        public double EpicenterLat { get; set; }     // EVLA
        public double EpicenterLon { get; set; }     // EVLO
        public double Depth { get; set; }            // EVDP (глубина в км)
        public float Magnitude { get; set; }         // MAG
    }
}