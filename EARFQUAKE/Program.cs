using EARFQUAKE;

class Program
{
    static void Main()
    {
        try
        {
            // 1. Загрузка данных
            var loader = new SacLoader();
            var sacFiles = loader.LoadFromJson(
                @"C:\Users\mihas\PycharmProjects\FullSacShit\SAC_PROCESSED\seismic_event.json.z"
            );

            if (sacFiles.Count == 0) return;

            var signalProcessor = new SignalProcessor();

            var spectrumAnalyzer = new SpectrumAnalyzer();

            var exampleSignal = sacFiles.FirstOrDefault();

            if (exampleSignal != null)
            {
                // --------------------------------------------------------
                // 1. Предобработка
                // --------------------------------------------------------

                double[] preprocessed =
                    signalProcessor.Preprocess(exampleSignal);

                // --------------------------------------------------------
                // 2. Bandpass 0.1–10 Hz
                // --------------------------------------------------------

                double[] filtered =
                    signalProcessor.FilterSignal(
                        preprocessed,
                        exampleSignal.SamplingRate,
                        0.1,
                        10.0,
                        4
                    );

                // --------------------------------------------------------
                // 3. FFT
                // --------------------------------------------------------

                SpectrumResult spectrum =
                    spectrumAnalyzer.CalculateFFT(
                        filtered,
                        exampleSignal.SamplingRate
                    );

                // --------------------------------------------------------
                // 4. Доминирующая частота
                // --------------------------------------------------------

                double dominantFrequency =
                    spectrumAnalyzer.FindDominantFrequency(
                        spectrum
                    );

                spectrumAnalyzer.PlotSpectrum(
                    exampleSignal
                );

                Console.WriteLine();
                Console.WriteLine("=== SPECTRAL ANALYSIS ===");

                Console.WriteLine(
                    $"Station:              {exampleSignal.Station}");

                Console.WriteLine(
                    $"Channel:              {exampleSignal.Channel}");

                Console.WriteLine(
                    $"FFT samples:          {spectrum.SampleCount}");

                Console.WriteLine(
                    $"Frequency resolution: {spectrum.FrequencyResolution:F6} Hz");

                Console.WriteLine(
                    $"Nyquist frequency:    {spectrum.NyquistFrequency:F2} Hz");

                Console.WriteLine(
                    $"Dominant frequency:   {dominantFrequency:F4} Hz");
            }

            //// 2. Базовый анализ
            //var analyzer = new SacAnalyzer();
            //analyzer.BasicAnalysis(sacFiles);

            //// 3. Графики
            //analyzer.PlotDistanceVsAmplitude(sacFiles);
            //analyzer.PlotStationMap(sacFiles);
            //analyzer.PlotBinnedGraph(sacFiles);

            //// 4. График для первой станции
            //if (sacFiles.Count > 0)
            //{
            //    analyzer.PlotExampleWaveform(sacFiles[0]);
            //}

            //// 5. Простой экспорт в CSV
            //ExportToCsv(sacFiles, "simple_analysis.csv");

            Console.WriteLine("\nГотово!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static void ExportToCsv(List<SacFile> sacFiles, string path)
    {
        using (var writer = new System.IO.StreamWriter(path))
        {
            writer.WriteLine("Station,Channel,Latitude,Longitude,DistanceKm,PeakAmplitude");

            foreach (var sac in sacFiles.Where(s => s.Latitude != -12345f))
            {
                writer.WriteLine($"{sac.Station},{sac.Channel},{sac.Latitude:F6},{sac.Longitude:F6},{sac.DistanceKm:F2},{sac.PeakAmplitude:E2}");
            }
        }
        Console.WriteLine($"Данные экспортированы в: {path}");
    }
}