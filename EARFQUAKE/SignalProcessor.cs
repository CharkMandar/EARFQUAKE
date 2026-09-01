using ScottPlot;

namespace EARFQUAKE
{
    // ========================================================================
    // РЕЗУЛЬТАТ АНАЛИЗА ОДНОГО СИГНАЛА
    // ========================================================================

    public class SignalAnalysisResult
    {
        public bool IsValid { get; set; }

        // Основные параметры
        public int SampleCount { get; set; }
        public double SamplingRate { get; set; }
        public double Delta { get; set; }
        public double Duration { get; set; }
        public double NyquistFrequency { get; set; }

        // Статистика сигнала
        public double Mean { get; set; }
        public double RMS { get; set; }
        public double StandardDeviation { get; set; }

        public double Min { get; set; }
        public double Max { get; set; }

        // Проверки качества
        public int NaNCount { get; set; }
        public int InfinityCount { get; set; }

        public string StatusMessage { get; set; } = "";
    }

    // ОБРАБОТКА ОДНОЙ СЕЙСМИЧЕСКОЙ ЗАПИСИ

    public class SignalProcessor
    {
        // ====================================================================
        // ПРЕДОБРАБОТКА СИГНАЛА
        // ====================================================================

        public double[] Preprocess(SacFile sacFile)
        {
            if (sacFile == null)
                throw new ArgumentNullException(nameof(sacFile));

            if (sacFile.DataSample == null ||
                sacFile.DataSample.Length == 0)
            {
                throw new ArgumentException(
                    "Сигнал отсутствует.",
                    nameof(sacFile)
                );
            }

            // float[] -> double[]
            double[] signal = sacFile.DataSample
                .Select(x => (double)x)
                .ToArray();

            // 1. Удаляем среднее значение
            RemoveMean(signal);

            // 2. Удаляем линейный тренд
            RemoveLinearTrend(signal);

            // 3. Применяем taper к краям сигнала
            ApplyTaper(signal);

            return signal;
        }


        // ====================================================================
        // УДАЛЕНИЕ СРЕДНЕГО ЗНАЧЕНИЯ (DEMEAN)
        // ====================================================================

        private void RemoveMean(double[] signal)
        {
            if (signal == null || signal.Length == 0)
                return;

            double sum = 0.0;

            foreach (double value in signal)
                sum += value;

            double mean = sum / signal.Length;

            for (int i = 0; i < signal.Length; i++)
                signal[i] -= mean;
        }


        // ====================================================================
        // УДАЛЕНИЕ ЛИНЕЙНОГО ТРЕНДА (DETREND)
        // ====================================================================

        private void RemoveLinearTrend(double[] signal)
        {
            if (signal == null || signal.Length < 2)
                return;

            int n = signal.Length;

            // Считаем координаты отсчётов:
            //
            // x = 0, 1, 2, ..., n-1
            //
            // Ищем прямую:
            //
            // y = a*x + b
            //
            // которая наилучшим образом описывает тренд сигнала.

            double sumX = 0.0;
            double sumY = 0.0;
            double sumXY = 0.0;
            double sumXX = 0.0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = signal[i];

                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumXX += x * x;
            }

            double denominator =
                n * sumXX - sumX * sumX;

            // Теоретически для n >= 2 этого быть не должно,
            // но оставляем защиту.
            if (Math.Abs(denominator) < double.Epsilon)
                return;

            double slope =
                (n * sumXY - sumX * sumY)
                / denominator;

            double intercept =
                (sumY - slope * sumX)
                / n;

            // Вычитаем найденный тренд
            for (int i = 0; i < n; i++)
            {
                double trend =
                    slope * i + intercept;

                signal[i] -= trend;
            }
        }


        // ====================================================================
        // TAPER
        // ====================================================================

        private void ApplyTaper(
            double[] signal,
            double taperFraction = 0.05)
        {
            if (signal == null || signal.Length < 2)
                return;

            if (taperFraction <= 0 ||
                taperFraction >= 0.5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(taperFraction),
                    "Доля taper должна быть > 0 и < 0.5."
                );
            }

            int n = signal.Length;

            // 5% длины сигнала с каждой стороны
            int taperSamples =
                (int)(n * taperFraction);

            if (taperSamples < 1)
                return;

            for (int i = 0; i < taperSamples; i++)
            {
                // Плавный cosine taper:
                //
                // w = 0.5 * (1 - cos(pi * x))
                //
                // где x изменяется от 0 до 1.

                double x =
                    (double)i / taperSamples;

                double weight =
                    0.5 *
                    (1.0 - Math.Cos(Math.PI * x));

                // Левая граница
                signal[i] *= weight;

                // Правая граница
                int rightIndex =
                    n - 1 - i;

                signal[rightIndex] *= weight;
            }
        }

        // ====================================================================
        // СРАВНЕНИЕ ИСХОДНОГО И ОБРАБОТАННОГО СИГНАЛА
        // ====================================================================

        public void PlotPreprocessingComparison(SacFile sacFile)
        {
            if (sacFile == null)
            {
                Console.WriteLine("SacFile == null");
                return;
            }

            if (sacFile.DataSample == null ||
                sacFile.DataSample.Length < 2)
            {
                Console.WriteLine("Недостаточно данных для графика");
                return;
            }

            if (double.IsNaN(sacFile.Delta) ||
                double.IsInfinity(sacFile.Delta) ||
                sacFile.Delta <= 0)
            {
                Console.WriteLine("Некорректный Delta");
                return;
            }

            // ------------------------------------------------------------
            // Исходный сигнал
            // ------------------------------------------------------------

            double[] rawSignal = sacFile.DataSample
                .Select(x => (double)x)
                .ToArray();

            // ------------------------------------------------------------
            // Обработанный сигнал
            // ------------------------------------------------------------

            double[] processedSignal = Preprocess(sacFile);

            // ------------------------------------------------------------
            // Временная шкала
            // ------------------------------------------------------------

            int sampleCount = rawSignal.Length;

            double[] time = new double[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                time[i] = i * sacFile.Delta;
            }

            // ------------------------------------------------------------
            // Создание графика
            // ------------------------------------------------------------

            var plt = new Plot();

            var rawPlot =
                plt.Add.Scatter(time, rawSignal);

            rawPlot.LineWidth = 1;
            rawPlot.Label = "Исходный сигнал";

            var processedPlot =
                plt.Add.Scatter(time, processedSignal);

            processedPlot.LineWidth = 1;
            processedPlot.Label = "После preprocessing";

            plt.Title(
                $"Preprocessing: {sacFile.Station}.{sacFile.Channel}"
            );

            plt.XLabel("Время (с)");
            plt.YLabel("Амплитуда");

            plt.ShowLegend();

            string fileName =
                $"preprocessing_{sacFile.Station}_{sacFile.Channel}.png";

            plt.Save(fileName, 1200, 800);

            Console.WriteLine(
                $"График preprocessing сохранен: {fileName}"
            );
        }

        public double[] FilterSignal(
    double[] signal,
    double samplingRate,
    double lowCut,
    double highCut,
    int order = 4)
        {
            return ButterworthFilter.Bandpass(
                signal,
                samplingRate,
                lowCut,
                highCut,
                order);
        }

        public SignalAnalysisResult Analyze(SacFile sacFile)
        {
            var result = new SignalAnalysisResult();

            // ------------------------------------------------------------
            // Проверка наличия данных
            // ------------------------------------------------------------

            if (sacFile == null)
            {
                result.IsValid = false;
                result.StatusMessage = "SacFile == null";
                return result;
            }

            if (sacFile.DataSample == null ||
                sacFile.DataSample.Length == 0)
            {
                result.IsValid = false;
                result.StatusMessage = "Сигнал отсутствует";
                return result;
            }

            // ------------------------------------------------------------
            // Основные параметры
            // ------------------------------------------------------------

            result.SampleCount = sacFile.DataSample.Length;
            result.SamplingRate = sacFile.SamplingRate;
            result.Delta = sacFile.Delta;

            // Проверяем частоту дискретизации
            if (double.IsNaN(result.SamplingRate) ||
                double.IsInfinity(result.SamplingRate) ||
                result.SamplingRate <= 0)
            {
                result.IsValid = false;
                result.StatusMessage =
                    "Некорректная частота дискретизации";
                return result;
            }

            // Проверяем Delta
            if (double.IsNaN(result.Delta) ||
                double.IsInfinity(result.Delta) ||
                result.Delta <= 0)
            {
                result.IsValid = false;
                result.StatusMessage =
                    "Некорректный Delta";
                return result;
            }

            // ------------------------------------------------------------
            // Временные характеристики
            // ------------------------------------------------------------

            result.Duration =
                result.SampleCount * result.Delta;

            result.NyquistFrequency =
                result.SamplingRate / 2.0;

            // ------------------------------------------------------------
            // Проверка значений
            // ------------------------------------------------------------

            int nanCount = 0;
            int infinityCount = 0;

            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            double sum = 0.0;
            double sumSquares = 0.0;

            int validCount = 0;

            foreach (float value in sacFile.DataSample)
            {
                double x = value;

                if (double.IsNaN(x))
                {
                    nanCount++;
                    continue;
                }

                if (double.IsInfinity(x))
                {
                    infinityCount++;
                    continue;
                }

                if (x < min)
                    min = x;

                if (x > max)
                    max = x;

                sum += x;
                sumSquares += x * x;

                validCount++;
            }

            result.NaNCount = nanCount;
            result.InfinityCount = infinityCount;

            // ------------------------------------------------------------
            // Если после проверки не осталось нормальных значений
            // ------------------------------------------------------------

            if (validCount == 0)
            {
                result.IsValid = false;
                result.StatusMessage =
                    "Сигнал не содержит корректных числовых значений";

                return result;
            }

            // ------------------------------------------------------------
            // Статистика
            // ------------------------------------------------------------

            result.Min = min;
            result.Max = max;

            result.Mean =
                sum / validCount;

            result.RMS =
                Math.Sqrt(sumSquares / validCount);

            // ------------------------------------------------------------
            // Дисперсия и стандартное отклонение
            // ------------------------------------------------------------

            double variance =
                (sumSquares / validCount)
                - (result.Mean * result.Mean);

            // Из-за погрешности floating point variance иногда
            // может получиться небольшим отрицательным числом.
            variance = Math.Max(0.0, variance);

            result.StandardDeviation =
                Math.Sqrt(variance);

            // ------------------------------------------------------------
            // Итоговая проверка
            // ------------------------------------------------------------

            if (nanCount > 0 || infinityCount > 0)
            {
                result.IsValid = false;

                result.StatusMessage =
                    $"Обнаружены некорректные значения: " +
                    $"NaN={nanCount}, Infinity={infinityCount}";
            }
            else
            {
                result.IsValid = true;
                result.StatusMessage = "OK";
            }

            return result;
        }

        #region ===отладка=== 

        public void PlotFilteringComparison(SacFile sacFile)
        {
            if (sacFile == null)
            {
                Console.WriteLine("SacFile == null");
                return;
            }

            if (sacFile.DataSample == null ||
                sacFile.DataSample.Length < 10)
            {
                Console.WriteLine("Недостаточно данных для фильтрации");
                return;
            }

            if (double.IsNaN(sacFile.SamplingRate) ||
                double.IsInfinity(sacFile.SamplingRate) ||
                sacFile.SamplingRate <= 0)
            {
                Console.WriteLine("Некорректная частота дискретизации");
                return;
            }

            // ------------------------------------------------------------
            // Параметры фильтра
            // ------------------------------------------------------------

            double lowCut = 0.01;
            double highCut = 10.0;
            int filterOrder = 4;

            double nyquist = sacFile.SamplingRate / 2.0;

            if (highCut >= nyquist)
            {
                Console.WriteLine(
                    $"Ошибка: highCut ({highCut:F2} Hz) " +
                    $">= Nyquist ({nyquist:F2} Hz)."
                );

                return;
            }

            // ------------------------------------------------------------
            // Исходный сигнал
            // ------------------------------------------------------------

            double[] rawSignal = sacFile.DataSample
                .Select(x => (double)x)
                .ToArray();

            // ------------------------------------------------------------
            // Preprocessing
            // ------------------------------------------------------------

            double[] preprocessedSignal = Preprocess(sacFile);

            // ------------------------------------------------------------
            // Band-pass фильтрация
            // ------------------------------------------------------------

            double[] filteredSignal = FilterSignal(
                preprocessedSignal,
                sacFile.SamplingRate,
                lowCut,
                highCut,
                filterOrder
            );

            // ------------------------------------------------------------
            // Временная шкала
            // ------------------------------------------------------------

            int sampleCount = rawSignal.Length;

            double[] time = new double[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                time[i] = i * sacFile.Delta;
            }

            // ------------------------------------------------------------
            // Создание графика
            // ------------------------------------------------------------

            var plt = new Plot();

            // Исходный сигнал
            var rawPlot =
                plt.Add.Scatter(time, rawSignal);

            rawPlot.LineWidth = 1;
            rawPlot.Label = "Исходный сигнал";

            // После preprocessing
            var preprocessedPlot =
                plt.Add.Scatter(time, preprocessedSignal);

            preprocessedPlot.LineWidth = 1;
            preprocessedPlot.Label = "После preprocessing";

            // После band-pass
            var filteredPlot =
                plt.Add.Scatter(time, filteredSignal);

            filteredPlot.LineWidth = 1;
            filteredPlot.Label =
                $"После band-pass ({lowCut:F2}–{highCut:F1} Hz)";

            // ------------------------------------------------------------
            // Оформление
            // ------------------------------------------------------------

            plt.Title(
                $"Фильтрация: " +
                $"{sacFile.Station}.{sacFile.Channel}"
            );

            plt.XLabel("Время (с)");
            plt.YLabel("Амплитуда");

            plt.ShowLegend();

            // ------------------------------------------------------------
            // Сохранение
            // ------------------------------------------------------------

            string fileName =
                $"filtering_{sacFile.Station}_{sacFile.Channel}.png";

            plt.Save(fileName, 1200, 800);

            Console.WriteLine(
                $"График фильтрации сохранен: {fileName}"
            );

            Console.WriteLine(
                $"  Sampling rate: {sacFile.SamplingRate:F2} Hz"
            );

            Console.WriteLine(
                $"  Nyquist:        {nyquist:F2} Hz"
            );

            Console.WriteLine(
                $"  Band-pass:      {lowCut:F2}–{highCut:F1} Hz"
            );

            Console.WriteLine(
                $"  Filter order:   {filterOrder}"
            );
        }

        // ====================================================================
        // ВЫВОД РЕЗУЛЬТАТА В КОНСОЛЬ
        // ====================================================================

        public void PrintAnalysis(
            SacFile sacFile,
            SignalAnalysisResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=== SIGNAL ANALYSIS ===");

            Console.WriteLine(
                $"Station:        {sacFile.Station}");

            Console.WriteLine(
                $"Channel:        {sacFile.Channel}");

            Console.WriteLine(
                $"Samples:        {result.SampleCount}");

            Console.WriteLine(
                $"Sampling rate:  {result.SamplingRate:F4} Hz");

            Console.WriteLine(
                $"Delta:          {result.Delta:F6} s");

            Console.WriteLine(
                $"Duration:       {result.Duration:F2} s");

            Console.WriteLine(
                $"Nyquist:        {result.NyquistFrequency:F2} Hz");

            Console.WriteLine();

            Console.WriteLine(
                $"Min:            {result.Min:E6}");

            Console.WriteLine(
                $"Max:            {result.Max:E6}");

            Console.WriteLine(
                $"Mean:           {result.Mean:E6}");

            Console.WriteLine(
                $"RMS:            {result.RMS:E6}");

            Console.WriteLine(
                $"Std deviation:  {result.StandardDeviation:E6}");

            Console.WriteLine();

            Console.WriteLine(
                $"NaN:            {result.NaNCount}");

            Console.WriteLine(
                $"Infinity:       {result.InfinityCount}");

            Console.WriteLine(
                $"Status:         {(result.IsValid ? "VALID" : "INVALID")}");

            Console.WriteLine(
                $"Message:        {result.StatusMessage}");
        }

#endregion
    }
}