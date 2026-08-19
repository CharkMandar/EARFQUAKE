using MathNet.Numerics.Statistics;
using ScottPlot;
using System.Security.Cryptography;

namespace EARFQUAKE
{
    public static class EnumerableExtentions
    {
        public static double Median(this IEnumerable<double> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var sorted = source.OrderBy(x => x).ToArray();
            int count = sorted.Length;

            if (count == 0)
                throw new InvalidOperationException("Последовательность не содержит элементов");

            if (count % 2 == 1)
                return sorted[count / 2];
            else
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        }
    }
    public class SacAnalyzer
    {
        public void BasicAnalysis(List<SacFile> sacFiles)
        {
            if (sacFiles.Count == 0)
            {
                Console.WriteLine("Нет данных для анализа");
                return;
            }

            // 1. Базовая статистика
            Console.WriteLine("\n=== БАЗОВЫЙ АНАЛИЗ ===");
            Console.WriteLine($"Всего файлов: {sacFiles.Count}");

            // Станции с координатами
            var withCoords = sacFiles.Where(s => s.Latitude != -12345f).ToList();
            Console.WriteLine($"С координатами: {withCoords.Count}");

            // 2. Дистанции
            if (withCoords.Count > 0)
            {
                var distances = withCoords.Select(s => s.DistanceKm).ToList();
                Console.WriteLine($"\nДистанции до эпицентра:");
                Console.WriteLine($"  Min: {distances.Min():F1} км");
                Console.WriteLine($"  Max: {distances.Max():F1} км");
                Console.WriteLine($"  Avg: {distances.Average():F1} км");
            }

            // 3. Амплитуды
            var withData = sacFiles.Where(s => s.DataSample != null && s.DataSample.Length > 0).ToList();
            if (withData.Count > 0)
            {
                var amplitudes = withData.Select(s => s.PeakAmplitude).ToList();
                Console.WriteLine($"\nПиковые амплитуды:");
                Console.WriteLine($"  Min: {amplitudes.Min():E2}");
                Console.WriteLine($"  Max: {amplitudes.Max():E2}");
                Console.WriteLine($"  Avg: {amplitudes.Average():E2}");
            }

            // 4. Пример одного файла
            if (sacFiles.Count > 0)
            {
                var example = sacFiles[0];
                Console.WriteLine($"\nПример файла:");
                Console.WriteLine($"  Станция: {example.Station}.{example.Channel}");
                Console.WriteLine($"  Координаты: {example.Latitude:F3}, {example.Longitude:F3}");
                Console.WriteLine($"  Дистанция: {example.DistanceKm:F1} км");
                Console.WriteLine($"  Амплитуда: {example.PeakAmplitude:E2}");
            }
        }

        public void PlotDistanceVsAmplitude(List<SacFile> sacFiles)
        {
            // Фильтруем файлы с координатами и данными
            var validFiles = sacFiles
                .Where(s => s.Latitude != -12345f &&
                           s.DataSample != null &&
                           s.DataSample.Length > 0 &&
                           s.PeakAmplitude > 0) // Исключаем нулевые амплитуды
                .ToList();

            if (validFiles.Count < 2)
            {
                Console.WriteLine("Недостаточно данных для графика");
                return;
            }

            // Подготовка данных
            double[] distances = validFiles.Select(s => s.DistanceKm).ToArray();
            double[] amplitudes = validFiles.Select(s => (double)s.PeakAmplitude).ToArray();

            // --- ФИЛЬТРАЦИЯ ВЫБРОСОВ (основная магия) ---
            // Сортируем амплитуды для вычисления квантилей
            var sortedAmplitudes = amplitudes.OrderBy(a => a).ToArray();
            int total = sortedAmplitudes.Length;

            // Вычисляем нижний и верхний квантили (отсекаем по 1% с каждого края)
            double lowerBound = sortedAmplitudes[(int)(total * 0.01)];
            double upperBound = sortedAmplitudes[(int)(total * 0.99)];

            // Фильтруем данные, оставляя только значения в пределах квантилей
            var filteredData = validFiles
                .Where((s, index) => amplitudes[index] >= lowerBound && amplitudes[index] <= upperBound)
                .ToList();

            double[] filteredDistances = filteredData.Select(s => s.DistanceKm).ToArray();
            double[] filteredAmplitudes = filteredData.Select(s => (double)s.PeakAmplitude).ToArray();

            Console.WriteLine($"\n=== ФИЛЬТРАЦИЯ ДЛЯ ГРАФИКА ===");
            Console.WriteLine($"Всего точек: {validFiles.Count}");
            Console.WriteLine($"После фильтрации (1%-99%): {filteredData.Count}");
            Console.WriteLine($"Диапазон амплитуд на графике: от {filteredAmplitudes.Min():E2} до {filteredAmplitudes.Max():E2}");

            // --- СОЗДАНИЕ ОСНОВНОГО ГРАФИКА (Расстояние vs Амплитуда) ---
            var plt = new Plot(); // Увеличиваем размер для двух графиков

            // График 1: Точечная диаграмма (разброс)
            double[] logYs = filteredAmplitudes.Select(y => Math.Log10(y)).ToArray();
            var scatterPlot = plt.Add.Scatter(filteredDistances, logYs);
            scatterPlot.LineWidth = 0;
            scatterPlot.MarkerSize = 5;
            scatterPlot.Color = Colors.Blue.WithAlpha(0.7); // Полупрозрачный
            scatterPlot.Label = "Амплитуда по станциям";

            // --- НАСТРОЙКА ОСЕЙ (важная часть) ---
            plt.Title("Зависимость амплитуды от дистанции (с фильтрацией выбросов)");
            plt.XLabel("Дистанция до эпицентра (км)");
            plt.YLabel("Пиковая амплитуда (логарифмическая шкала)");

            plt.ShowLegend();

            // Сохраняем основной график
            plt.Save("distance_vs_amplitude_filtered.png", 1200, 800);
            Console.WriteLine($"\nОсновной график сохранен: distance_vs_amplitude_filtered.png");

            //// --- ДОПОЛНИТЕЛЬНЫЙ ГРАФИК: РАСПРЕДЕЛЕНИЕ АМПЛИТУД ---
            //// Полезно понять, какие значения амплитуд являются "нормальными"
            //var pltHist = new Plot();
            //pltHist.Title("Распределение пиковых амплитуд (гистограмма)");
            //pltHist.XLabel("Пиковая амплитуда");
            //pltHist.YLabel("Количество станций");
            //
            //ScottPlot.Statistics.Histogram = new ScottPlot.Statistics.Histogram(amplitudes);
            //
            //var hist = pltHist.Add.Histogram(amplitudes);
            //hist.Width = 2;
            //hist.FillColor = Colors.LightBlue;
            //hist.LineColor = Colors.Blue;
            //
            //// На оси X тоже используем логарифмический масштаб из-за большого разброса
            //pltHist.Axes.Bottom.Scale = new ScottPlot.AxisScales.Log10();
            //
            //var histXGenerator = new ScottPlot.TickGenerators.Log10();
            //histXGenerator.LabelFormatter = pos => Math.Pow(10, pos).ToString("E1");
            //pltHist.Axes.Bottom.TickGenerator = histXGenerator;
            //
            //pltHist.SaveFig("amplitude_histogram.png");
            //Console.WriteLine($"Гистограмма сохранена: amplitude_histogram.png");
        }

        public void PlotStationMap(List<SacFile> sacFiles)
        {
            // Станции с координатами
            var stationsWithCoords = sacFiles
                .Where(s => s.Latitude != -12345f)
                .GroupBy(s => s.Station)
                .Select(g => g.First())
                .ToList();

            if (stationsWithCoords.Count == 0) return;

            var plt = new Plot();

            plt.Title("Карта станций");
            plt.XLabel("Долгота");
            plt.YLabel("Широта");

            double[] lons = stationsWithCoords.Select(s => (double)s.Longitude).ToArray();
            double[] lats = stationsWithCoords.Select(s => (double)s.Latitude).ToArray();

            var scatter = plt.Add.Scatter(lons, lats);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 8;
            scatter.Color = Colors.Red;

            // Добавляем эпицентр
            var epicenter = plt.Add.Marker(153.3, 54.9);
            epicenter.MarkerSize = 15;
            epicenter.Color = Colors.Green;
            //epicenter. = "Эпицентр";

            plt.Save("station_map.png", 800, 600);
            Console.WriteLine("Карта станций сохранена: station_map.png");
        }

        public void PlotExampleWaveform(SacFile sacFile)
        {
            if (sacFile.DataSample == null || sacFile.DataSample.Length < 10)
            {
                Console.WriteLine("Нет данных для графика сигнала");
                return;
            }

            var plt = new Plot();

            plt.Title($"Сигнал: {sacFile.Station}.{sacFile.Channel}");
            plt.XLabel("Время (отсчеты)");
            plt.YLabel("Амплитуда");

            // Создаем временную ось
            double[] time = new double[sacFile.DataSample.Length];
            for (int i = 0; i < time.Length; i++)
            {
                time[i] = i * sacFile.Delta;
            }

            double[] data = sacFile.DataSample.Select(v => (double)v).ToArray();

            var signal = plt.Add.Scatter(time, data);
            signal.LineWidth = 1;
            signal.Color = Colors.Blue;

            // Отмечаем пиковую амплитуду
            var maxPoint = plt.Add.Marker(
                Array.IndexOf(sacFile.DataSample, sacFile.DataSample.Max()) * sacFile.Delta,
                sacFile.PeakAmplitude
            );
            maxPoint.MarkerSize = 10;
            maxPoint.Color = Colors.Red;

            plt.Save($"waveform_{sacFile.Station}.png", 800, 400);
            Console.WriteLine($"График сигнала сохранен: waveform_{sacFile.Station}.png");
        }

        public void PlotBinnedGraph(List<SacFile> sacFiles)
        {

            List<(double min, double max)> bins = new()
            {
                (1000, 3000),
                (3000, 5000),
                (5000, 7000),
                (7000, 9000),
                (9000, 12000),
                (12000, 15000),
                (15000, 18000),
                (18000, 21000) 
            };
            

            var validFiles = sacFiles
           .Where(s => s.Latitude != -12345f &&
                      s.DataSample != null &&
                      s.DataSample.Length > 0 &&
                      s.PeakAmplitude > 0) // Исключаем нулевые амплитуды
           .ToList();

            if (validFiles.Count < 2)
            {
                Console.WriteLine("Недостаточно данных для графика");
                return;
            }
            var binnedData = new List<(double midDistance, double medianAmplitude)>();
            // Подготовка данных
            double[] distances = validFiles.Select(s => s.DistanceKm).ToArray();
            double[] amplitudes = validFiles.Select(s => (double)s.PeakAmplitude).ToArray();

            // --- ФИЛЬТРАЦИЯ ВЫБРОСОВ (основная магия) ---
            // Сортируем амплитуды для вычисления квантилей
            var sortedAmplitudes = amplitudes.OrderBy(a => a).ToArray();
            int total = sortedAmplitudes.Length;

            // Вычисляем нижний и верхний квантили (отсекаем по 1% с каждого края)
            double lowerBound = sortedAmplitudes[(int)(total * 0.01)];
            double upperBound = sortedAmplitudes[(int)(total * 0.99)];

            // Фильтруем данные, оставляя только значения в пределах квантилей
            var filteredData = validFiles
                .Where((s, index) => amplitudes[index] >= lowerBound && amplitudes[index] <= upperBound)
                .ToList();


            foreach (var bin in bins)
            {
                var stationsInBin = filteredData.Where(s => s.DistanceKm >= bin.min && s.DistanceKm < bin.max).ToList();
                if (stationsInBin.Count > 0)
                {
                    double mid = (bin.min + bin.max) / 2;
                    double median = stationsInBin.Select(s => s.PeakAmplitude).Median(); 
                    binnedData.Add((mid, median));
                }
            }

            double[] filteredDistances = binnedData.Select(b => b.midDistance).ToArray();
            double[] filteredAmplitudes = binnedData.Select(b => b.medianAmplitude).ToArray();         

            // --- СОЗДАНИЕ ОСНОВНОГО ГРАФИКА (Расстояние vs Амплитуда) ---
            var plt = new Plot(); // Увеличиваем размер для двух графиков

            // График 1: Точечная диаграмма (разброс)
            double[] logYs = filteredAmplitudes.Select(y => Math.Log10(y)).ToArray();
            var scatterPlot = plt.Add.Scatter(filteredDistances, logYs);
            scatterPlot.LineWidth = 0;
            scatterPlot.MarkerSize = 15;
            scatterPlot.Color = Colors.Red.WithAlpha(0.7); // Полупрозрачный
            scatterPlot.Label = "Амплитуда по станциям";

            // --- НАСТРОЙКА ОСЕЙ (важная часть) ---
            plt.Title("Усредненные данные и тренд затухания");
            plt.XLabel("Дистанция до эпицентра (км)");
            plt.YLabel("Пиковая амплитуда (логарифмическая шкала)");

            plt.ShowLegend();

            // Сохраняем основной график
            plt.Save("distance_vs_amplitude_median.png", 1200, 800);
            Console.WriteLine($"\nОсновной график сохранен: distance_vs_amplitude_medain.png");
        }

    }
}