namespace EARFQUAKE
{
    public class SeismicSpectralAnalyzer
    {
        private readonly SignalProcessor signalProcessor;
        private readonly SpectrumAnalyzer spectrumAnalyzer;

        public SeismicSpectralAnalyzer()
        {
            signalProcessor = new SignalProcessor();
            spectrumAnalyzer = new SpectrumAnalyzer();
        }

        // ====================================================================
        // ПОЛНЫЙ СПЕКТРАЛЬНЫЙ АНАЛИЗ ОДНОЙ SAC-ЗАПИСИ
        // ====================================================================

        public StationSpectralResult? AnalyzeSacFile(
            SacFile sacFile)
        {
            if (sacFile == null)
            {
                return null;
            }

            if (sacFile.DataSample == null ||
                sacFile.DataSample.Length < 2)
            {
                return null;
            }

            if (double.IsNaN(sacFile.SamplingRate) ||
                double.IsInfinity(sacFile.SamplingRate) ||
                sacFile.SamplingRate <= 0)
            {
                return null;
            }

            // ------------------------------------------------------------
            // 1. Предобработка
            // ------------------------------------------------------------

            double[] preprocessed =
                signalProcessor.Preprocess(
                    sacFile
                );

            if (preprocessed == null ||
                preprocessed.Length < 2)
            {
                return null;
            }

            // ------------------------------------------------------------
            // 2. Bandpass 0.1–10 Hz
            // ------------------------------------------------------------

            double nyquist =
    sacFile.SamplingRate / 2.0;

            double highCut =
                Math.Min(10.0, nyquist * 0.9);

            if (highCut <= 0.1)
            {
                return null;
            }

            double[] filtered =
                signalProcessor.FilterSignal(
                    preprocessed,
                    sacFile.SamplingRate,
                    0.1,
                    highCut,
                    4
                );

            if (filtered == null ||
                filtered.Length < 2)
            {
                return null;
            }

            // ------------------------------------------------------------
            // 3. FFT
            // ------------------------------------------------------------

            SpectrumResult spectrum =
                spectrumAnalyzer.CalculateFFT(
                    filtered,
                    sacFile.SamplingRate
                );

            if (!spectrum.IsValid)
            {
                return null;
            }

            // ------------------------------------------------------------
            // 4. Спектральные характеристики
            // ------------------------------------------------------------

            SpectralFeatures features =
                spectrumAnalyzer.AnalyzeSpectrum(
                    spectrum
                );

            // ------------------------------------------------------------
            // 5. Формируем результат
            // ------------------------------------------------------------

            return new StationSpectralResult
            {
                Network = sacFile.Network,

                Station = sacFile.Station,

                Channel = sacFile.Channel,

                DistanceKm = sacFile.DistanceKm,

                Location = sacFile.Location,

                Features = features
            };
        }

        public List<StationSpectralResult> AnalyzeBatch(
    List<SacFile> records)
        {
            var results = new List<StationSpectralResult>();

            foreach (var record in records)
            {
                var result = AnalyzeSacFile(record);

                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }
    }
}