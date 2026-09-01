using EARFQUAKE;

class Program
{
    static void Main()
    {
        try
        {
            // ============================================================
            // 1. Загрузка данных
            // ============================================================

            var loader = new SacLoader();

            var sacFiles = loader.LoadFromJson(
                @"C:\Users\mihas\PycharmProjects\FullSacShit\SAC_PROCESSED\seismic_event.json.z"
            );

            if (sacFiles.Count == 0)
            {
                Console.WriteLine("Данные не загружены.");
                return;
            }

            Console.WriteLine(
                $"Загружено записей: {sacFiles.Count}"
            );

            // ============================================================
            // 2. Очистка
            // ============================================================

            var cleaner = new SeismicDataCleaner();

            var cleanedRecords =
                cleaner.RemoveDuplicates(sacFiles);

            Console.WriteLine(
                $"После очистки:   {cleanedRecords.Count}"
            );

            // ============================================================
            // 3. Спектральный анализ
            // ============================================================

            var spectralAnalyzer =
                new SeismicSpectralAnalyzer();

            var results =
                new List<StationSpectralResult>();

            foreach (var record in cleanedRecords)
            {
                var result =
                    spectralAnalyzer.AnalyzeSacFile(record);

                if (result != null)
                {
                    results.Add(result);
                }
            }

            Console.WriteLine(
                $"Проанализировано: {results.Count}"
            );

            // ============================================================
            // 4. Визуализация
            // ============================================================

            var visualizer =
                new SeismicSpectralVisualizer();

            Console.WriteLine();
            Console.WriteLine(
                "=== SPECTRAL VISUALIZATION ==="
            );

            visualizer.PlotDominantFrequencyVsDistance(
                results
            );

            visualizer.PlotSpectralCentroidVsDistance(
                results
            );

            visualizer.PlotSpectralBandwidthVsDistance(
                results
            );

            visualizer.PlotSpectralEnergyVsDistance(
                results
            );

            // ============================================================
            // 5. Завершение
            // ============================================================

            Console.WriteLine();
            Console.WriteLine(
                "Визуализация завершена."
            );

            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("Ошибка:");
            Console.WriteLine(ex.Message);

            Console.ReadKey();
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

}