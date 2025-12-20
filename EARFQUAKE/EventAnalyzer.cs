using ICSharpCode.SharpZipLib.Tar;
using ScottPlot;
using System.Net.ServerSentEvents;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EARFQUAKE
{
    public class EventAnalyzer
    {
        private static List<string> ExtractTar(string tarPath, string outputDir)
        {
            var files = new List<string>();

            using (var stream = File.OpenRead(tarPath))
            using (var tar = TarArchive.CreateInputTarArchive(stream))
            {
                tar.ExtractContents(outputDir);
            }

            // Собираем список .sac файлов
            files.AddRange(Directory.GetFiles(outputDir, "*.sac", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(outputDir, "*.SAC", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(outputDir, "*.SACA", SearchOption.AllDirectories));

            return files;
        }
        public static string ExtractStationCode(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "UNKNOWN";

            // Убираем расширение .SACA
            string name = Path.GetFileNameWithoutExtension(fileName);

            //// Разделяем по точкам
            //string[] parts = name.Split('.');
            //
            //// В вашем формате станция всегда на второй позиции (индекс 1)
            //// NET(0).STA(1).LOC(2).CHAN(3).QUAL(4).YEAR(5).DOY(6).TIME(7)
            //if (parts.Length >= 2)
            //{
            //    return parts[1]; // CMB
            //}

            return name;
        }

        public List<SacFile> ProcessSimple(string tarPath)
        {
            var results = new List<SacFile>();

            // 1. Распаковать
            string tempDir = $"temp_{Guid.NewGuid()}";
            var files = ExtractTar(tarPath, tempDir);

            // 2. Только BHZ файлы для простоты
            var zhFiles = files.Where(f => Path.GetFileName(f).Contains("BHZ")).ToList();


            Console.WriteLine($"Найдено {zhFiles.Count} BHZ записей");

            // 3. Обработать каждую
            foreach (var file in zhFiles)
            {
                try
                {
                    SacFile sac = SacParser.ParseBinarySac(file);
                    //sac.FilePath = ExtractStationCode(file);
                    results.Add(sac);
                    //Console.WriteLine($"  {sac.Kstnm}: {sac.DistanceKm}km, A={sac.PeakAmplitude}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            var filteredData = results.Where(s =>
            s.PeakAmplitude > 0 &&
            s.DistanceKm > 50 && // Убираем слишком близкие станции
            s.PeakAmplitude < 1e8 && // Убираем явные артефакты
            !double.IsNaN(s.DistanceKm) &&
            !double.IsInfinity(s.PeakAmplitude)
            ).ToList();
            // 4. Очистка
            Directory.Delete(tempDir, true);

            return filteredData.OrderBy(r => r.DistanceKm).ToList();
        }

        public void DrawGraph(List<SacFile> data)
        {
            double[] distanceData = data.Select(x => x.DistanceKm).ToArray();
            double[] amplitudeData = data.Select(x => Convert.ToDouble(x.PeakAmplitude)).ToArray();
            var logDistance = distanceData.Select(x => Math.Log10(x));
            var logAmplitude = amplitudeData.Select(x => Math.Log10(x));

            var maxRecord = data.OrderByDescending(x => x.PeakAmplitude).First();

            Console.WriteLine($" Амплитуда: {maxRecord.PeakAmplitude}, Широта: {maxRecord.Stla}, Долгота: {maxRecord.Stlo}");

            //График зависимости амплитуды от дистанции
            var plotLinear = new Plot();
            plotLinear.Title("Затухание амплитуды с расстоянием");
            plotLinear.XLabel("Расстояние от эпицентра (км)");
            plotLinear.YLabel("Пиковая амплитуда");
            var scatterLinear = plotLinear.Add.Scatter(distanceData, amplitudeData);
            scatterLinear.LegendText = "Наблюдения";
            scatterLinear.MarkerSize = 7;
            scatterLinear.LineWidth = 0;

            plotLinear.ShowLegend();
            plotLinear.SavePng("amplitude_linear.png", 800, 600);
        }
    }
}