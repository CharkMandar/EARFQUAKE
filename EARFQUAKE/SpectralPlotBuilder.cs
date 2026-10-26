using ScottPlot;

namespace EARFQUAKE
{
    public enum SpectralFeature
    {
        DominantFrequency,
        SpectralCentroid,
        SpectralBandwidth,
        SpectralEnergy
    }

    /// <summary>
    /// Строит графики «признак от расстояния». Ничего не сохраняет и не пишет в консоль:
    /// возвращает Plot, а форма сама решает, показать его или сохранить.
    /// </summary>
    public static class SpectralPlotBuilder
    {
        public static string DisplayName(SpectralFeature f) => f switch
        {
            SpectralFeature.DominantFrequency => "Dominant Frequency (Hz)",
            SpectralFeature.SpectralCentroid => "Spectral Centroid (Hz)",
            SpectralFeature.SpectralBandwidth => "Spectral Bandwidth (Hz)",
            SpectralFeature.SpectralEnergy => "log10(Spectral Energy)",
            _ => f.ToString()
        };

        private static double GetValue(SpectralFeatures s, SpectralFeature f) => f switch
        {
            SpectralFeature.DominantFrequency => s.DominantFrequency,
            SpectralFeature.SpectralCentroid => s.SpectralCentroid,
            SpectralFeature.SpectralBandwidth => s.SpectralBandwidth,
            SpectralFeature.SpectralEnergy => s.SpectralEnergy,
            _ => double.NaN
        };

        /// <param name="channel">null — все компоненты, иначе, например, "BHZ".</param>
        public static Plot BuildVsDistance(
            IEnumerable<StationSpectralResult> results,
            SpectralFeature feature,
            string? channel = null)
        {
            var plt = new Plot();
            string yLabel = DisplayName(feature);
            bool logValue = feature == SpectralFeature.SpectralEnergy;

            var points = results
                .Where(r => r != null && r.Features != null)
                .Where(r => channel == null || r.Channel == channel)
                .Where(r => double.IsFinite(r.DistanceKm) && r.DistanceKm > 0)
                .Select(r => (Dist: r.DistanceKm, Value: GetValue(r.Features, feature)))
                .Where(p => double.IsFinite(p.Value) && p.Value > 0)
                .ToList();

            plt.Title($"{yLabel} vs Distance" + (channel != null ? $" ({channel})" : ""));
            plt.XLabel("Epicentral Distance (km, logarithmic scale)");
            plt.YLabel(yLabel);

            if (points.Count == 0)
            {
                plt.Title("Нет подходящих данных для графика");
                return plt;
            }

            double[] xs = points.Select(p => Math.Log10(p.Dist)).ToArray();
            double[] ys = points
                .Select(p => logValue ? Math.Log10(p.Value) : p.Value)
                .ToArray();

            var scatter = plt.Add.Scatter(xs, ys);
            scatter.LineWidth = 0;      // только точки
            scatter.MarkerSize = 6;

            // Подписи по оси X: полудекады от минимума до максимума в данных
            double lo = Math.Floor(xs.Min() * 2) / 2;
            double hi = Math.Ceiling(xs.Max() * 2) / 2;

            var ticks = new List<double>();
            for (double t = lo; t <= hi + 1e-9; t += 0.5)
                ticks.Add(t);

            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                ticks.ToArray(),
                ticks.Select(t => Math.Round(Math.Pow(10, t)).ToString("0")).ToArray());

            plt.Axes.SetLimitsX(lo, hi);
            plt.Axes.AutoScaleY();

            return plt;
        }
    }
}
