using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using ScottPlot;


namespace EARFQUAKE
{
    // ========================================================================
    // РЕЗУЛЬТАТ СПЕКТРАЛЬНОГО АНАЛИЗА
    // ========================================================================

    public class SpectrumResult
    {
        public double[] Frequencies { get; set; } = Array.Empty<double>();

        public double[] Amplitudes { get; set; } = Array.Empty<double>();

        public int SampleCount { get; set; }

        public double SamplingRate { get; set; }

        public double FrequencyResolution { get; set; }

        public double NyquistFrequency { get; set; }

        public bool IsValid { get; set; }

        public string StatusMessage { get; set; } = "";
    }

    // ========================================================================
    // СПЕКТРАЛЬНЫЕ ХАРАКТЕРИСТИКИ
    // ========================================================================

    public class SpectralFeatures
    {
        public double DominantFrequency {get; set; }
        public double SpectralCentroid  {get; set; }
        public double SpectralBandwidth {get; set; }
        public double SpectralEnergy { get; set; }
    }


    // ========================================================================
    // СПЕКТРАЛЬНЫЙ АНАЛИЗАТОР
    // ========================================================================

    public class SpectrumAnalyzer
    {
        // ====================================================================
        // FFT
        // ====================================================================

        public SpectrumResult CalculateFFT(
    double[] signal,
    double samplingRate)
        {
            var result = new SpectrumResult();

            // ------------------------------------------------------------
            // Проверка сигнала
            // ------------------------------------------------------------

            if (signal == null || signal.Length == 0)
            {
                result.IsValid = false;
                result.StatusMessage = "Сигнал пустой";
                return result;
            }

            // ------------------------------------------------------------
            // Проверка sampling rate
            // ------------------------------------------------------------

            if (double.IsNaN(samplingRate) ||
                double.IsInfinity(samplingRate) ||
                samplingRate <= 0)
            {
                result.IsValid = false;
                result.StatusMessage =
                    "Некорректная частота дискретизации";
                return result;
            }

            // ------------------------------------------------------------
            // Размер FFT
            //
            // Используем все имеющиеся отсчёты.
            // MathNet поддерживает FFT для произвольной длины.
            // ------------------------------------------------------------

            int fftSize = signal.Length;

            // ------------------------------------------------------------
            // Слишком короткий сигнал
            // ------------------------------------------------------------

            if (fftSize < 2)
            {
                result.IsValid = false;
                result.StatusMessage =
                    "Недостаточно отсчётов для FFT";
                return result;
            }

            // ------------------------------------------------------------
            // Создаём комплексный массив
            // ------------------------------------------------------------

            Complex[] fftData = new Complex[fftSize];

            for (int i = 0; i < fftSize; i++)
            {
                fftData[i] =
                    new Complex(signal[i], 0.0);
            }

            // ------------------------------------------------------------
            // Выполняем FFT
            // ------------------------------------------------------------

            Fourier.Forward(
                fftData,
                FourierOptions.Matlab
            );

            // ------------------------------------------------------------
            // Односторонний спектр
            //
            // Для вещественного сигнала достаточно половины спектра:
            //
            // 0 ... Nyquist
            // ------------------------------------------------------------

            int spectrumSize =
                fftSize / 2 + 1;

            double[] frequencies =
                new double[spectrumSize];

            double[] amplitudes =
                new double[spectrumSize];

            // ------------------------------------------------------------
            // Частотное разрешение
            // ------------------------------------------------------------

            double frequencyResolution =
                samplingRate / fftSize;

            // ------------------------------------------------------------
            // Амплитудный спектр
            // ------------------------------------------------------------

            for (int k = 0; k < spectrumSize; k++)
            {
                frequencies[k] =
                    k * frequencyResolution;

                double magnitude =
                    fftData[k].Magnitude;

                // Нормировка амплитуды
                double amplitude =
                    magnitude / fftSize;

                // Для одностороннего спектра
                // удваиваем амплитуды всех компонент,
                // кроме DC и Nyquist.
                if (k != 0 &&
                    k != fftSize / 2)
                {
                    amplitude *= 2.0;
                }

                amplitudes[k] = amplitude;
            }

            // ------------------------------------------------------------
            // Заполняем результат
            // ------------------------------------------------------------

            result.Frequencies = frequencies;
            result.Amplitudes = amplitudes;

            result.SampleCount = fftSize;

            result.SamplingRate =
                samplingRate;

            result.FrequencyResolution =
                frequencyResolution;

            result.NyquistFrequency =
                samplingRate / 2.0;

            result.IsValid = true;

            result.StatusMessage = "OK";

            return result;
        }

        // ========================================================================
        // ПОИСК ДОМИНИРУЮЩЕЙ ЧАСТОТЫ
        // ========================================================================

        public double FindDominantFrequency(
    SpectrumResult spectrum)
        {
            if (spectrum == null ||
                !spectrum.IsValid ||
                spectrum.Frequencies == null ||
                spectrum.Amplitudes == null ||
                spectrum.Frequencies.Length < 2 ||
                spectrum.Amplitudes.Length < 2)
            {
                return double.NaN;
            }

            int maxIndex = 1;

            for (int i = 2; i < spectrum.Amplitudes.Length; i++)
            {
                if (spectrum.Amplitudes[i] >
                    spectrum.Amplitudes[maxIndex])
                {
                    maxIndex = i;
                }
            }

            return spectrum.Frequencies[maxIndex];
        }

        // ========================================================================
        // СПЕКТРАЛЬНЫЙ ЦЕНТРОИД
        // ========================================================================

        public double CalculateSpectralCentroid(
            SpectrumResult spectrum)
        {
            if (spectrum == null ||
                !spectrum.IsValid ||
                spectrum.Frequencies == null ||
                spectrum.Amplitudes == null ||
                spectrum.Frequencies.Length < 2 ||
                spectrum.Amplitudes.Length < 2 ||
                spectrum.Frequencies.Length != spectrum.Amplitudes.Length)
            {
                return double.NaN;
            }

            double weightedFrequencySum = 0.0;
            double amplitudeSum = 0.0;

            for (int i = 1; i < spectrum.Frequencies.Length; i++)
            {
                double frequency = spectrum.Frequencies[i];
                double amplitude = spectrum.Amplitudes[i];

                weightedFrequencySum += frequency * amplitude;
                amplitudeSum += amplitude;
            }

            if (amplitudeSum <= 0.0 ||
                double.IsNaN(amplitudeSum) ||
                double.IsInfinity(amplitudeSum))
            {
                return double.NaN;
            }

            return weightedFrequencySum / amplitudeSum;
        }

        // ========================================================================
        // СПЕКТРАЛЬНАЯ ШИРИНА
        // ========================================================================

        public double CalculateSpectralBandwidth(
            SpectrumResult spectrum)
        {
            if (spectrum == null ||
                !spectrum.IsValid ||
                spectrum.Frequencies == null ||
                spectrum.Amplitudes == null ||
                spectrum.Frequencies.Length < 2 ||
                spectrum.Amplitudes.Length < 2 ||
                spectrum.Frequencies.Length != spectrum.Amplitudes.Length)
            {
                return double.NaN;
            }

            double centroid =
                CalculateSpectralCentroid(spectrum);

            if (double.IsNaN(centroid) ||
                double.IsInfinity(centroid))
            {
                return double.NaN;
            }

            double weightedSquaredDeviation = 0.0;
            double amplitudeSum = 0.0;

            for (int i = 1; i < spectrum.Frequencies.Length; i++)
            {
                double frequency = spectrum.Frequencies[i];
                double amplitude = spectrum.Amplitudes[i];

                double deviation =
                    frequency - centroid;

                weightedSquaredDeviation +=
                    amplitude * deviation * deviation;

                amplitudeSum += amplitude;
            }

            if (amplitudeSum <= 0.0 ||
                double.IsNaN(amplitudeSum) ||
                double.IsInfinity(amplitudeSum))
            {
                return double.NaN;
            }

            return Math.Sqrt(
                weightedSquaredDeviation / amplitudeSum
            );
        }

        // ========================================================================
        // СПЕКТРАЛЬНАЯ ЭНЕРГИЯ
        // ========================================================================

        public double CalculateSpectralEnergy(
            SpectrumResult spectrum)
        {
            if (spectrum == null ||
                !spectrum.IsValid ||
                spectrum.Amplitudes == null ||
                spectrum.Amplitudes.Length < 2)
            {
                return double.NaN;
            }

            double energy = 0.0;

            for (int i = 1; i < spectrum.Amplitudes.Length; i++)
            {
                double amplitude =
                    spectrum.Amplitudes[i];

                energy += amplitude * amplitude;
            }

            if (double.IsNaN(energy) ||
                double.IsInfinity(energy))
            {
                return double.NaN;
            }

            return energy;
        }

        // ========================================================================
        // АНАЛИЗ СПЕКТРАЛЬНЫХ ХАРАКТЕРИСТИК
        // ========================================================================

        public SpectralFeatures AnalyzeSpectrum(
            SpectrumResult spectrum)
        {
            if (spectrum == null ||
                !spectrum.IsValid)
            {
                return new SpectralFeatures
                {
                    DominantFrequency = double.NaN,
                    SpectralCentroid = double.NaN,
                    SpectralBandwidth = double.NaN,
                    SpectralEnergy = double.NaN
                };
            }

            return new SpectralFeatures
            {
                DominantFrequency =
                    FindDominantFrequency(spectrum),

                SpectralCentroid =
                    CalculateSpectralCentroid(spectrum),

                SpectralBandwidth =
                    CalculateSpectralBandwidth(spectrum),

                SpectralEnergy =
                    CalculateSpectralEnergy(spectrum)
            };
        }

        // ========================================================================
        // ГРАФИК АМПЛИТУДНОГО СПЕКТРА
        // ========================================================================

        public void PlotSpectrum(
    SpectrumResult spectrum,
    SacFile sacFile)
        {
            if (sacFile == null)
            {
                Console.WriteLine("SacFile == null");
                return;
            }

            if (spectrum == null || !spectrum.IsValid)
            {
                Console.WriteLine(
                    "Некорректный результат спектрального анализа"
                );
                return;
            }

            if (spectrum.Frequencies == null ||
                spectrum.Amplitudes == null ||
                spectrum.Frequencies.Length < 2)
            {
                Console.WriteLine(
                    "Недостаточно данных для построения спектра"
                );
                return;
            }

            // ------------------------------------------------------------
            // 1. Убираем DC-компоненту
            // ------------------------------------------------------------

            double[] positiveFrequencies =
                spectrum.Frequencies
                    .Skip(1)
                    .ToArray();

            double[] positiveAmplitudes =
                spectrum.Amplitudes
                    .Skip(1)
                    .ToArray();

            if (positiveFrequencies.Length == 0)
            {
                Console.WriteLine(
                    "Нет положительных частот"
                );
                return;
            }

            // ------------------------------------------------------------
            // 2. Переводим частоту в log10(f)
            // ------------------------------------------------------------

            double[] logFrequencies =
                positiveFrequencies
                    .Select(f => Math.Log10(f))
                    .ToArray();

            // ------------------------------------------------------------
            // 3. Создаём график
            // ------------------------------------------------------------

            var plt = new Plot();

            var spectrumPlot =
                plt.Add.Scatter(
                    logFrequencies,
                    positiveAmplitudes
                );

            spectrumPlot.LineWidth = 1;

            // ------------------------------------------------------------
            // 4. Логарифмическая ось X
            // ------------------------------------------------------------

            double minFrequency =
                positiveFrequencies.First();

            double maxFrequency =
                spectrum.NyquistFrequency;

            plt.Axes.SetLimitsX(
                Math.Log10(minFrequency),
                Math.Log10(maxFrequency)
            );

            // ------------------------------------------------------------
            // 5. Подписи оси X
            // ------------------------------------------------------------

            var tickPositions = new double[]
            {
        -2,
        -1,
         0,
         1
            };

            var tickLabels = new string[]
            {
        "0.01",
        "0.1",
        "1",
        "10"
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
                $"Amplitude Spectrum: " +
                $"{sacFile.Station}.{sacFile.Channel}"
            );

            plt.XLabel("Frequency (Hz)");
            plt.YLabel("Amplitude");

            // ------------------------------------------------------------
            // 7. Сохранение
            // ------------------------------------------------------------

            string fileName =
                $"spectrum_{sacFile.Station}_{sacFile.Channel}.png";

            plt.Save(
                fileName,
                1200,
                800
            );

            Console.WriteLine(
                $"Спектр сохранен: {fileName}"
            );
        }
    }
}