using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Tar;

namespace MagisterProject
{
    class Program
    {
        const string tar = @"C:\Users\Mikhail\Downloads\2023-02-06-mww75-turkey.tar";
        const string name = @"C:\Users\mihas\Downloads\2023-02-06-mww75-turkey\BK.CMB.00.BHZ.Q.2023.037.103739.SACA";
        static void Main()
        {

            var sac = SacFile.Read(name);

            Console.WriteLine($"Станция: {sac.Kstnm}, Компонента: {sac.Kcmpnm}");
            Console.WriteLine($"Расстояние: {sac.DistanceKm} км");
            Console.WriteLine($"Пиковая амплитуда: {sac.PeakAmplitude}");
            Console.WriteLine($"Время начала: {sac.StartTime}");
            Console.WriteLine(sac.RmsAmplitude + "   " + sac.PeakAmplitude);
            //
            //Console.WriteLine($"Запись содержит {sac.Data.Length} точек");
            //Console.WriteLine($"Частота дискретизации: {1 / sac.Delta} Hz");
            //Console.WriteLine($"Диапазон амплитуд: {sac.Data.Min():F3} - {sac.Data.Max():F3}");
            //
            //Console.WriteLine("Первые 100 точек");
            //for (int i = 0; i < 100; i++)
            //{
            //    Console.WriteLine($"{sac.Data[i]}");
            //}

            //AnalyzeEvent(tar);
        }

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

        private static void AnalyzeEvent(string tarPath)
        {

            // Распаковываем во временную папку
            string tempDir = $"temp_{DateTime.Now:yyyyMMdd_HHmmss}";
            var sacFiles = ExtractTar(tarPath, tempDir);

            Console.WriteLine($"Найдено {sacFiles.Count} SAC файлов");

            // Анализируем каждый файл
            foreach (var file in sacFiles.Take(5)) // Для примера первые 5 файлов
            {
                try
                {
                    var sac = SacFile.Read(file);
                    Console.WriteLine($"{Path.GetFileName(file)}: {sac.Data.Length} samples, Delta={sac.Delta}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка чтения {file}: {ex.Message}");
                }
            }

            // Очистка
            Directory.Delete(tempDir, true);
        }

        public static double CalculateDistance(double stationLat, double stationLon, double epicenterLat, double epicenterLon)
        {
            // Используем формулу гаверсинусов или Vincenty
            // MathNet.Spatial может помочь


            double distanceInKm = 0;
            return distanceInKm;
        }
    }
}
