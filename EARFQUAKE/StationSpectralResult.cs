using System;
using System.Collections.Generic;
using System.Text;

namespace EARFQUAKE
{
    public class StationSpectralResult
    {
        public string Network { get; set; } = "";

        public string Station { get; set; } = "";

        public string Channel { get; set; } = "";

        public double DistanceKm { get; set; } = double.NaN;

        public SpectralFeatures Features { get; set; } = new();

        public double SamplingRate { get; set; } = double.NaN;

        public string Location { get; set; } = "";
    }
}
