using EARTHQUAKE;

class Program
{
    static void Main()
    {
        try
        {
            // 1. Загрузка данных
            var loader = new SacLoader();
            var sacFiles = loader.LoadFromJson(
                @"C:\Users\mihas\PycharmProjects\FullSacShit\SAC_PROCESSED\sac_metadata.json.z"
            );

            if (sacFiles.Count == 0) return;

            // 2. Базовый анализ
            var analyzer = new SacAnalyzer();
            analyzer.BasicAnalysis(sacFiles);

            // 3. Графики
            analyzer.PlotDistanceVsAmplitude(sacFiles);
            analyzer.PlotStationMap(sacFiles);

            // 4. График для первой станции
            if (sacFiles.Count > 0)
            {
                analyzer.PlotExampleWaveform(sacFiles[0]);
            }

            // 5. Простой экспорт в CSV
            ExportToCsv(sacFiles, "simple_analysis.csv");

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