// Код вызова для консольного приложения. После перехода на WinForms не актуален, лежит на всякий случай для возможной отладки.



//using EARFQUAKE;

//var runner = CreateRunner();

//// Результаты кладём в папку пользователя: она всегда доступна для записи
//string outDir = Path.Combine(
//    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//    "EARFQUAKE", "output");
//Directory.CreateDirectory(outDir);

//Console.WriteLine("Скачивание и обработка, это может занять несколько минут...");

//string json = await runner.RunAsync(
//    new[] { "download", "--time", "2012-08-14T02:59:00",
//            "--duration", "3600", "--out", outDir },
//    new Progress<string>(Console.WriteLine));

//var files = new SacLoader().LoadFromJson(json);
//Console.WriteLine($"\nЗагружено записей: {files.Count}");

//foreach (var f in files.Take(5))
//    Console.WriteLine(
//        $"{f.Network}.{f.Station}.{f.Channel}  " +
//        $"станция: {f.Latitude:F2}, {f.Longitude:F2}  " +
//        $"расстояние: {f.DistanceKm:F0} км  " +
//        $"отсчётов: {f.DataSample.Length}");

//Console.WriteLine("\nНажми любую клавишу, чтобы закрыть окно...");
//Console.ReadKey();

//// Работает и в разработке (через Python из .venv), и у пользователя (через sacfetch.exe)
//static PythonRunner CreateRunner()
//{
//    string exe = Path.Combine(AppContext.BaseDirectory, "sacfetch", "sacfetch.exe");
//    if (File.Exists(exe))
//        return new PythonRunner(exe);

//#if DEBUG
//    // Только для разработки: в Release-сборку этот код не попадает
//    return new PythonRunner(
//        @"C:\Users\mihas\PycharmProjects\FullSacShit\.venv\Scripts\python.exe",
//        @"C:\Users\mihas\source\repos\EARFQUAKE\python\SacProcessor.py");
//#else
//    throw new FileNotFoundException(
//        "Не найден sacfetch\\sacfetch.exe рядом с программой. " +
//        "Распакуйте архив целиком и запустите EARFQUAKE.exe из распакованной папки.",
//        exe);
//#endif
//}

//using EARFQUAKE;

//class Program
//{
//    static async Task Main()
//    {
//        try
//        {
//            // ============================================================
//            // 1. Загрузка данных
//            // ============================================================

//            var loader = new SacLoader();

//            //var sacFiles = loader.LoadFromJson(
//            //    @"C:\Users\mihas\PycharmProjects\FullSacShit\SAC_PROCESSED\seismic_event.json.z"
//            //);

//            //if (sacFiles.Count == 0)
//            //{
//            //    Console.WriteLine("Данные не загружены.");
//            //    return;
//            //}

//            //Console.WriteLine(
//            //    $"Загружено записей: {sacFiles.Count}"
//            //);

//            var runner = new PythonRunner(
//                @"C:\Users\mihas\PycharmProjects\FullSacShit\.venv\Scripts\python.exe",
//                @"C:\Users\mihas\PycharmProjects\FullSacShit\SacProcessor.py");

//            string outDir = @"C:\Users\mihas\PycharmProjects\FullSacShit\SAC_PROCESSED";
//            Directory.CreateDirectory(outDir);

//            string json = await runner.RunAsync(
//                new[] { "download", "--time", "2012-08-14T02:59:00",
//            "--duration", "3600", "--out", outDir },
//                new Progress<string>(Console.WriteLine));

//            var sacFiles = new SacLoader().LoadFromJson(json);
//            Console.WriteLine($"\nЗагружено записей: {sacFiles.Count}");

//            foreach (var f in sacFiles.Take(5))
//                Console.WriteLine(
//                    $"{f.Network}.{f.Station}.{f.Channel}  " +
//                    $"станция: {f.Latitude:F2}, {f.Longitude:F2}  " +
//                    $"расстояние: {f.DistanceKm:F0} км  " +
//                    $"отсчётов: {f.DataSample.Length}");

//            // ============================================================
//            // 2. Очистка
//            // ============================================================

//            var cleaner = new SeismicDataCleaner();

//            var cleanedRecords =
//                cleaner.RemoveDuplicates(sacFiles);

//            Console.WriteLine(
//                $"После очистки:   {cleanedRecords.Count}"
//            );

//            // ============================================================
//            // 3. Спектральный анализ
//            // ============================================================

//            var spectralAnalyzer =
//                new SeismicSpectralAnalyzer();

//            var results =
//                new List<StationSpectralResult>();

//            foreach (var record in cleanedRecords)
//            {
//                var result =
//                    spectralAnalyzer.AnalyzeSacFile(record);

//                if (result != null)
//                {
//                    results.Add(result);
//                }
//            }

//            Console.WriteLine(
//                $"Проанализировано: {results.Count}"
//            );

//            var analyzer = new SacAnalyzer();

//            Console.WriteLine();
//            Console.WriteLine(
//                "=== AMPLITUDE VS DISTANCE ==="
//            );

//            analyzer.PlotDistanceVsAmplitude(cleanedRecords);

//            // ============================================================
//            // 4. Вывод результатов спектрального анализа
//            // ============================================================

//            Console.WriteLine();
//            Console.WriteLine(
//                "=== РЕЗУЛЬТАТЫ СПЕКТРАЛЬНОГО АНАЛИЗА ==="
//            );

//            Console.WriteLine(
//                $"{"Станция",-10}" +
//                $"{"Канал",-10}" +
//                $"{"Доминирующая частота",-22}" +
//                $"{"Спектральный центроид",-22}" +
//                $"{"Спектральная ширина",-18}" +
//                $"{"Энергия сигнала",-18}"
//            );

//            Console.WriteLine(
//                new string('-', 100)
//            );

//            foreach (var result in results.Take(10))
//            {
//                Console.WriteLine(
//                    $"{result.Station,-10}" +
//                    $"{result.Channel,-10}" +
//                    $"{result.Features.DominantFrequency,-22:F4}" +
//                    $"{result.Features.SpectralCentroid,-22:F4}" +
//                    $"{result.Features.SpectralBandwidth,-18:F4}" +
//                    $"{result.Features.SpectralEnergy,-18:E4}"
//                );
//            }

//            // ============================================================
//            // 5. Визуализация
//            // ============================================================

//            var visualizer =
//                new SeismicSpectralVisualizer();

//            Console.WriteLine();
//            Console.WriteLine(
//                "=== SPECTRAL VISUALIZATION ==="
//            );

//            visualizer.PlotDominantFrequencyVsDistance(
//                results
//            );

//            visualizer.PlotSpectralCentroidVsDistance(
//                results
//            );

//            visualizer.PlotSpectralBandwidthVsDistance(
//                results
//            );

//            visualizer.PlotSpectralEnergyVsDistance(
//                results
//            );

//            // ============================================================
//            // 6. Завершение
//            // ============================================================

//            Console.WriteLine();
//            Console.WriteLine(
//                "Визуализация завершена."
//            );

//            Console.ReadKey();
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine();
//            Console.WriteLine("Ошибка:");
//            Console.WriteLine(ex.Message);

//            Console.ReadKey();
//        }
//    }


//    static void ExportToCsv(List<SacFile> sacFiles, string path)
//    {
//        using (var writer = new System.IO.StreamWriter(path))
//        {
//            writer.WriteLine("Station,Channel,Latitude,Longitude,DistanceKm,PeakAmplitude");

//            foreach (var sac in sacFiles.Where(s => s.Latitude != -12345f))
//            {
//                writer.WriteLine($"{sac.Station},{sac.Channel},{sac.Latitude:F6},{sac.Longitude:F6},{sac.DistanceKm:F2},{sac.PeakAmplitude:E2}");
//            }
//        }
//        Console.WriteLine($"Данные экспортированы в: {path}");
//    }



//    //// 2. Базовый анализ
//    //var analyzer = new SacAnalyzer();
//    //analyzer.BasicAnalysis(sacFiles);

//    //// 3. Графики
//    //analyzer.PlotDistanceVsAmplitude(sacFiles);
//    //analyzer.PlotStationMap(sacFiles);
//    //analyzer.PlotBinnedGraph(sacFiles);

//    //// 4. График для первой станции
//    //if (sacFiles.Count > 0)
//    //{
//    //    analyzer.PlotExampleWaveform(sacFiles[0]);
//    //}

//    //// 5. Простой экспорт в CSV
//    //ExportToCsv(sacFiles, "simple_analysis.csv");

//}