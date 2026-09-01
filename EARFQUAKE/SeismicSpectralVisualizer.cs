using ScottPlot;

namespace EARFQUAKE
{
    public class SeismicSpectralVisualizer
    {
        // ====================================================================
        // РАССТОЯНИЕ → ДОМИНИРУЮЩАЯ ЧАСТОТА
        // ====================================================================

        public void PlotDominantFrequencyVsDistance(
    List<StationSpectralResult> results)
        {
            if (results == null || results.Count == 0)
            {
                Console.WriteLine(
                    "Нет результатов для визуализации."
                );

                return;
            }

            // ------------------------------------------------------------
            // 1. Берём только вертикальную компоненту BHZ
            // ------------------------------------------------------------

            var data = results
                .Where(x =>
                    x != null &&
                    x.Channel == "BHZ" &&
                    !double.IsNaN(x.DistanceKm) &&
                    x.DistanceKm > 0 &&
                    !double.IsNaN(
                        x.Features.DominantFrequency
                    ) && x.Features.DominantFrequency < 5)
                .OrderBy(x => x.DistanceKm)
                .ToList();

            if (data.Count == 0)
            {
                Console.WriteLine(
                    "Нет подходящих данных для графика."
                );

                return;
            }

            // ------------------------------------------------------------
            // 2. Логарифмируем расстояние
            // ------------------------------------------------------------

            double[] logDistances =
                data.Select(
                    x => Math.Log10(x.DistanceKm)
                ).ToArray();

            double[] frequencies =
                data.Select(
                    x => x.Features.DominantFrequency
                ).ToArray();

            // ------------------------------------------------------------
            // 3. Создаём график
            // ------------------------------------------------------------

            var plt = new Plot();

            var scatter =
                plt.Add.Scatter(
                    logDistances,
                    frequencies
                );

            // Только точки, без соединяющей линии
            scatter.LineWidth = 0;

            // ------------------------------------------------------------
            // 4. Логарифмическая шкала расстояния
            // ------------------------------------------------------------

            double minDistance =
                data.Min(x => x.DistanceKm);

            double maxDistance =
                data.Max(x => x.DistanceKm);

            plt.Axes.SetLimitsX(
                Math.Log10(minDistance),
                Math.Log10(maxDistance)
            );

            // ------------------------------------------------------------
            // 5. Подписи логарифмической оси X
            // ------------------------------------------------------------

            double[] tickPositions =
                new double[]
                {
            3,
            3.5,
            4,
            4.5
                };

            string[] tickLabels =
                new string[]
                {
            "10³",
            "10³·⁵",
            "10⁴",
            "10⁴·⁵"
                };

            plt.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.NumericManual(
                    tickPositions,
                    tickLabels
                );

            // ------------------------------------------------------------
            // 6. Подписи
            // ------------------------------------------------------------

            plt.Title(
                "Dominant Frequency vs Epicentral Distance"
            );

            plt.XLabel(
                "Epicentral Distance (km, logarithmic scale)"
            );

            plt.YLabel(
                "Dominant Frequency (Hz)"
            );

            // ------------------------------------------------------------
            // 7. Сохраняем
            // ------------------------------------------------------------

            string fileName =
                "dominant_frequency_vs_distance_log.png";

            plt.Save(
                fileName,
                1200,
                800
            );

            Console.WriteLine(
                $"График сохранён: {fileName}"
            );
        }

        public void PlotSpectralCentroidVsDistance(
    List<StationSpectralResult> results)
        {
            if (results == null || results.Count == 0)
            {
                Console.WriteLine(
                    "Нет данных для построения Spectral Centroid."
                );
                return;
            }

            var points = results
                .Where(x =>
                    x.Features != null &&
                    !double.IsNaN(x.DistanceKm) &&
                    !double.IsInfinity(x.DistanceKm) &&
                    x.DistanceKm > 0 &&
                    !double.IsNaN(x.Features.SpectralCentroid) &&
                    !double.IsInfinity(x.Features.SpectralCentroid) &&
                    x.Features.SpectralCentroid > 0)
                .ToList();

            if (points.Count == 0)
            {
                Console.WriteLine(
                    "Нет корректных данных для Spectral Centroid."
                );
                return;
            }

            double[] distances =
                points.Select(x => Math.Log10(x.DistanceKm)).ToArray();

            double[] centroids =
                points.Select(x => x.Features.SpectralCentroid).ToArray();

            var plt = new Plot();

            var scatter =
                plt.Add.Scatter(
                    distances,
                    centroids
                );

            scatter.LineWidth = 0;

            plt.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.NumericManual(
                    new double[]
                    {
                3,
                3.5,
                4,
                4.5
                    },
                    new string[]
                    {
                "1000",
                "3162",
                "10000",
                "31623"
                    }
                );

            plt.Title(
                "Spectral Centroid vs Distance"
            );

            plt.XLabel("Distance (km)");
            plt.YLabel("Spectral Centroid (Hz)");

            plt.Save(
                "spectral_centroid_vs_distance_log.png",
                1200,
                800
            );

            Console.WriteLine(
                "График сохранён: spectral_centroid_vs_distance_log.png"
            );
        }


        public void PlotSpectralBandwidthVsDistance(
            List<StationSpectralResult> results)
        {
            if (results == null || results.Count == 0)
            {
                Console.WriteLine(
                    "Нет данных для построения Spectral Bandwidth."
                );
                return;
            }

            var points = results
                .Where(x =>
                    x.Features != null &&
                    !double.IsNaN(x.DistanceKm) &&
                    !double.IsInfinity(x.DistanceKm) &&
                    x.DistanceKm > 0 &&
                    !double.IsNaN(x.Features.SpectralBandwidth) &&
                    !double.IsInfinity(x.Features.SpectralBandwidth) &&
                    x.Features.SpectralBandwidth > 0)
                .ToList();

            if (points.Count == 0)
            {
                Console.WriteLine(
                    "Нет корректных данных для Spectral Bandwidth."
                );
                return;
            }

            double[] distances =
                points.Select(x => Math.Log10(x.DistanceKm)).ToArray();

            double[] bandwidths =
                points.Select(x => x.Features.SpectralBandwidth).ToArray();

            var plt = new Plot();

            var scatter =
                plt.Add.Scatter(
                    distances,
                    bandwidths
                );

            scatter.LineWidth = 0;

            plt.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.NumericManual(
                    new double[]
                    {
                3,
                3.5,
                4,
                4.5
                    },
                    new string[]
                    {
                "1000",
                "3162",
                "10000",
                "31623"
                    }
                );

            plt.Title(
                "Spectral Bandwidth vs Distance"
            );

            plt.XLabel("Distance (km)");
            plt.YLabel("Spectral Bandwidth (Hz)");

            plt.Save(
                "spectral_bandwidth_vs_distance_log.png",
                1200,
                800
            );

            Console.WriteLine(
                "График сохранён: spectral_bandwidth_vs_distance_log.png"
            );
        }


        public void PlotSpectralEnergyVsDistance(
            List<StationSpectralResult> results)
        {
            if (results == null || results.Count == 0)
            {
                Console.WriteLine(
                    "Нет данных для построения Spectral Energy."
                );
                return;
            }

            var points = results
                .Where(x =>
                    x.Features != null &&
                    !double.IsNaN(x.DistanceKm) &&
                    !double.IsInfinity(x.DistanceKm) &&
                    x.DistanceKm > 0 &&
                    !double.IsNaN(x.Features.SpectralEnergy) &&
                    !double.IsInfinity(x.Features.SpectralEnergy) &&
                    x.Features.SpectralEnergy > 0)
                .ToList();

            if (points.Count == 0)
            {
                Console.WriteLine(
                    "Нет корректных данных для Spectral Energy."
                );
                return;
            }

            double[] distances =
                points.Select(x => Math.Log10(x.DistanceKm)).ToArray();

            double[] energies =
                points.Select(x => Math.Log10(x.Features.SpectralEnergy)).ToArray();

            var plt = new Plot();

            var scatter =
                plt.Add.Scatter(
                    distances,
                    energies
                );

            scatter.LineWidth = 0;

            plt.Axes.Bottom.TickGenerator =
                new ScottPlot.TickGenerators.NumericManual(
                    new double[]
                    {
                3,
                3.5,
                4,
                4.5
                    },
                    new string[]
                    {
                "1000",
                "3162",
                "10000",
                "31623"
                    }
                );

            plt.Title(
                "Spectral Energy vs Distance"
            );

            plt.XLabel("Distance (km)");
            plt.YLabel("log10(Spectral Energy)");

            plt.Save(
                "spectral_energy_vs_distance_log.png",
                1200,
                800
            );

            Console.WriteLine(
                "График сохранён: spectral_energy_vs_distance_log.png"
            );
        }
    }
}