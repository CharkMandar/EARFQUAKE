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

            Console.WriteLine();

            // ================================================================
            // 2. Очистка дубликатов
            // ================================================================

            var cleaner = new SeismicDataCleaner();

            var cleanedRecords =
                cleaner.RemoveDuplicates(sacFiles);

            Console.WriteLine(
                $"После очистки: {cleanedRecords.Count}"
            );

            // ================================================================
            // 3. Спектральный анализ
            // ================================================================

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

            // ================================================================
            // 4. Итоговая информация
            // ================================================================

            Console.WriteLine();
            Console.WriteLine(
                "=== SEISMIC SPECTRAL ANALYSIS ==="
            );

            Console.WriteLine(
                $"Исходных записей:       {sacFiles.Count}"
            );

            Console.WriteLine(
                $"Уникальных записей:     {cleanedRecords.Count}"
            );

            Console.WriteLine(
                $"Успешно проанализировано: {results.Count}"
            );

            Console.WriteLine(
                $"Не обработано:           " +
                $"{cleanedRecords.Count - results.Count}"
            );

            Console.WriteLine();

            Console.WriteLine(
                $"Уникальных станций: " +
                $"{results.Select(r => r.Station).Distinct().Count()}"
            );

            Console.WriteLine(
                $"Рассчитываемые характеристики:"
            );

            Console.WriteLine(
                "  • Dominant Frequency"
            );

            Console.WriteLine(
                "  • Spectral Centroid"
            );

            Console.WriteLine(
                "  • Spectral Bandwidth"
            );

            Console.WriteLine(
                "  • Spectral Energy"
            );

            // ================================================================
            // 5. Первые результаты
            // ================================================================

            Console.WriteLine();
            Console.WriteLine(
                "=== ПРИМЕР РЕЗУЛЬТАТОВ ==="
            );

            foreach (var result in results.Take(10))
            {
                Console.WriteLine();

                Console.WriteLine(
                    $"{result.Network}." +
                    $"{result.Station}." +
                    $"{result.Location}." +
                    $"{result.Channel}"
                );

                Console.WriteLine(
                    $"Distance:   " +
                    $"{result.DistanceKm:F2} km"
                );

                Console.WriteLine(
                    $"Dominant:   " +
                    $"{result.Features.DominantFrequency:F4} Hz"
                );

                Console.WriteLine(
                    $"Centroid:   " +
                    $"{result.Features.SpectralCentroid:F4} Hz"
                );

                Console.WriteLine(
                    $"Bandwidth:  " +
                    $"{result.Features.SpectralBandwidth:F4} Hz"
                );

                Console.WriteLine(
                    $"Energy:     " +
                    $"{result.Features.SpectralEnergy:E6}"
                );
            }

            // ================================================================
            // 6. Завершение
            // ================================================================

            Console.WriteLine();
            Console.WriteLine(
                "Спектральный анализ завершён."
            );

            Console.WriteLine(
                "Следующий этап: сравнение характеристик " +
                "между станциями и визуализация."
            );

            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Критическая ошибка: {ex.Message}"
            );
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