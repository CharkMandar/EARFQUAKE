
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace EARFQUAKE
{
    class Program
    {
        const string tar = @"C:\Users\mihas\Downloads\Telegram Desktop\2023-02-06-mww75-turkey.tar";
        const string name = @"C:\Users\mihas\Downloads\2023-02-06-mww75-turkey\BK.CMB.00.BHZ.Q.2023.037.103739.SACA";
        const string name2 = @"C:\Users\mihas\Downloads\123\BK.CMB.00.BHN.Q.2023.037.103739.SAC";
        static void Main()
        {

            //var sac = SacFile.Read(name2);

            //AnalyzeSacAscii(name);
            //Console.WriteLine($"Станция: {sac.Kstnm}, Компонента: {sac.Kcmpnm}");
            //Console.WriteLine($"Расстояние: {sac.DistanceKm} км");
            //Console.WriteLine($"Пиковая амплитуда: {sac.PeakAmplitude}");
            //Console.WriteLine($"Время начала: {sac.StartTime}");
            //Console.WriteLine(sac.RmsAmplitude + "   " + sac.PeakAmplitude);
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
            // 1. Простейший тест
            //var sac = ParseSimple(name);
            //Console.WriteLine("=== АНАЛИЗ СТАНЦИИ ===");
            //Console.WriteLine($"Координаты станции: {sac.Stla}°N, {sac.Stlo}°E, {sac.Stel} м");
            //Console.WriteLine($"Координаты события: {sac.Evla}°N, {sac.Evlo}°E, {sac.Evdp} км");
            //Console.WriteLine($"Расстояние: {sac.DistanceKm} км");
            //Console.WriteLine($"Пиковая амплитуда: {sac.PeakAmplitude}");
            //Console.WriteLine($"RMS амплитуда: {sac.RmsAmplitude}");
            //Console.WriteLine($"Время начала: {sac.StartTime}");
            //Console.WriteLine($"Тип данных: {(sac.Idep == 6 ? "velocity" : sac.Idep == 7 ? "acceleration" : "unknown")}");
            //Console.WriteLine($"Имя компоненты: {sac.Kcmpnm}, код станции: {sac.Kstnm}");


            // 2. Посчитайте расстояние
            //sac.DistanceKm = sac.FindDistance();
            //Console.WriteLine($"Distance: {sac.DistanceKm} km");
            string tempDir = $"temp_{DateTime.Now:yyyyMMdd_HHmmss}";
            var sacFiles = ExtractTar(tar, tempDir);

            Directory.Delete(tempDir, true);

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
                    var sac = ParseSimple(file);
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

        public static SacFile ParseSimple(string filename)
        {
            var sac = new SacFile();
            var lines = File.ReadAllLines(filename);

            // Читаем все числа
            var allNumbers = new List<float>();
            foreach (var line in lines)
            {
                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float num))
                        allNumbers.Add(num);
                }
            }

            Console.WriteLine($"Всего чисел: {allNumbers.Count}");

            if (allNumbers.Count >= 140)
            {
                // === FLOAT ПОЛЯ ===
                sac.Delta = allNumbers[0];    // 0: Delta = 0.025
                sac.B = allNumbers[5];        // 5: B
                sac.A = allNumbers[8];        // 8: A
                sac.Stla = allNumbers[31];    // 31: Stla ⭐ ИСПРАВЛЕНО!
                sac.Stlo = allNumbers[32];    // 32: Stlo ⭐ ИСПРАВЛЕНО!
                sac.Stel = allNumbers[33];    // 33: Stel (высота станции)
                sac.Evla = allNumbers[35];    // 35: Evla ⭐ ИСПРАВЛЕНО!
                sac.Evlo = allNumbers[36];    // 36: Evlo (долгота события)
                sac.Evel = allNumbers[37];    // 37: Evel (высота события)
                sac.Evdp = allNumbers[39];    // 39: Evdp (глубина события)
                sac.Mag = allNumbers[40];     // 40: Mag (магнитуда)

                // === INT ПОЛЯ ===
                // Конвертируем float → int
                int nzyear = (int)allNumbers[70];   // 70: NZYEAR
                int nzjday = (int)allNumbers[71];   // 71: NZJDAY
                int nzhour = (int)allNumbers[72];   // 72: NZHOUR
                int nzmin = (int)allNumbers[73];    // 73: NZMIN
                int nzsec = (int)allNumbers[74];    // 74: NZSEC
                int nzmsec = (int)allNumbers[75];   // 75: NZMSEC

                // Время события (из вашего дампа: 2023 37 10 37 39)
                sac.StartTime = new DateTime(nzyear, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddDays(nzjday - 1)
                    .AddHours(nzhour)
                    .AddMinutes(nzmin)
                    .AddSeconds(nzsec)
                    .AddMilliseconds(nzmsec);

                sac.Npts = (int)allNumbers[79];     // 79: NPTS
                sac.Idep = (int)allNumbers[83];     // 83: IDEP

                // === ДАННЫЕ ===
                int headerSize = 140; // 70 float + 70 int
                if (allNumbers.Count > headerSize)
                {
                    int expected = sac.Npts;
                    int available = allNumbers.Count - headerSize;

                    Console.WriteLine($"Ожидаем данных: {expected}, доступно: {available}");

                    int takeCount = Math.Min(expected, available);
                    sac.Data = allNumbers.Skip(headerSize).Take(takeCount).Select(n => (float)n).ToArray();
                    sac.Npts = sac.Data.Length;

                    // Амплитуды
                    if (sac.Data.Length > 0)
                    {
                        sac.PeakAmplitude = sac.Data.Max(x => Math.Abs(x));
                        double sumOfSquares = sac.Data.Sum(x => (double)x * x);
                        sac.RmsAmplitude = (float)Math.Sqrt(sumOfSquares / sac.Data.Length);
                    }
                }

                // === РАСЧЕТЫ ===
                sac.DistanceKm = sac.FindDistance();
            }

            return sac;
        }
    }
}
