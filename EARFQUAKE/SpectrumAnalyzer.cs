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
            // Для FFT желательно использовать степень двойки
            // ------------------------------------------------------------

            int originalLength = signal.Length;

            int fftSize = 1;

            while (fftSize * 2 <= originalLength)
            {
                fftSize *= 2;
            }

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
                spectrum.Frequencies.Length == 0 ||
                spectrum.Amplitudes.Length == 0)
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
        // ГРАФИК АМПЛИТУДНОГО СПЕКТРА
        // ========================================================================

        public void PlotSpectrum(SacFile sacFile)
        {
            if (sacFile == null)
            {
                Console.WriteLine("SacFile == null");
                return;
            }

            if (sacFile.DataSample == null ||
                sacFile.DataSample.Length < 2)
            {
                Console.WriteLine(
                    "Недостаточно данных для спектрального анализа"
                );
                return;
            }

            if (double.IsNaN(sacFile.SamplingRate) ||
                double.IsInfinity(sacFile.SamplingRate) ||
                sacFile.SamplingRate <= 0)
            {
                Console.WriteLine(
                    "Некорректная частота дискретизации"
                );
                return;
            }

            // ------------------------------------------------------------
            // 1. Предобработка
            // ------------------------------------------------------------

            var sp = new SignalProcessor();
            double[] signal = sp.Preprocess(sacFile);

            // ------------------------------------------------------------
            // 2. Определяем размер FFT
            // ------------------------------------------------------------

            int fftSize = Math.Min(
                8192,
                signal.Length
            );

            if (fftSize < 2)
            {
                Console.WriteLine(
                    "Недостаточный размер FFT"
                );
                return;
            }

            // ------------------------------------------------------------
            // 3. Берём часть сигнала для FFT
            // ------------------------------------------------------------

            Complex[] fftInput = signal
                .Take(fftSize)
                .Select(x => new Complex(x, 0))
                .ToArray();

            Fourier.Forward(
                fftInput,
                FourierOptions.Matlab
            );

            // ------------------------------------------------------------
            // 5. Формируем амплитудный спектр
            // ------------------------------------------------------------

            int spectrumLength =
                fftSize / 2 + 1;

            double[] frequencies =
                new double[spectrumLength];

            double[] amplitudes =
                new double[spectrumLength];

            for (int i = 0; i < spectrumLength; i++)
            {
                frequencies[i] =
                    i * sacFile.SamplingRate / fftSize;

                amplitudes[i] =
                    fftInput[i].Magnitude;
            }

            // ------------------------------------------------------------
            // 6. Убираем частоту 0 Hz
            // ------------------------------------------------------------

            // На логарифмической шкале
            // log10(0) не существует.

            double[] positiveFrequencies =
                frequencies
                    .Skip(1)
                    .ToArray();

            double[] positiveAmplitudes =
                amplitudes
                    .Skip(1)
                    .ToArray();

            // ------------------------------------------------------------
            // 7. Переводим X в log10(f)
            // ------------------------------------------------------------

            double[] logFrequencies =
                positiveFrequencies
                    .Select(f => Math.Log10(f))
                    .ToArray();

            // ------------------------------------------------------------
            // 8. Создаём график
            // ------------------------------------------------------------

            var plt = new Plot();

            var spectrumPlot =
                plt.Add.Scatter(
                    logFrequencies,
                    positiveAmplitudes
                );

            spectrumPlot.LineWidth = 1;

            // ------------------------------------------------------------
            // 9. Настройка оси X
            // ------------------------------------------------------------

            // Мы сами используем log10(f),
            // поэтому координаты:
            //
            // log10(0.01) = -2
            // log10(0.1)  = -1
            // log10(1)    =  0
            // log10(10)   =  1
            //
            // Но подписи сделаем нормальными.

            plt.Axes.SetLimitsX(
                Math.Log10(0.01),
                Math.Log10(sacFile.SamplingRate / 2.0)
            );

            // ------------------------------------------------------------
            // 10. Подписи логарифмической оси
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
            // 11. Названия графика и осей
            // ------------------------------------------------------------

            plt.Title(
                $"Amplitude Spectrum: " +
                $"{sacFile.Station}.{sacFile.Channel}"
            );

            plt.XLabel("Frequency (Hz)");
            plt.YLabel("Amplitude");

            // ------------------------------------------------------------
            // 12. Сохраняем график
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