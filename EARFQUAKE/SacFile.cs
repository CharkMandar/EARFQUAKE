using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MagisterProject
{
    public class SacFile
    {
        public float[] Data { get; set; }   // Данные

        public float Delta { get; set; }    // Период дискретизации

        public string Kstnm { get; set; }   // Код станции
        public float Stla { get; set; }     // Широта станции
        public float Stlo { get; set; }     // Долгота станции
        public float Stel { get; set; }     // Высота станции
        public int Npts { get; set; }   // Количество точек


        // 4. Тип данных (velocity/acceleration/displacement)
        public int Idep { get; set; }   // ТИП ДАННЫХ: 6=velocity, 7=acceleration 


        // 5. Информация о событии 
        public float Evla { get; set; }     // Широта эпицентра
        public float Evlo { get; set; }     // Долгота эпицентра
        public float Evel { get; set; }     // Высота эпицентра
        public float Evdp { get; set; }     // Глубина эпицентра (км)
        public float Mag { get; set; }      // Магнитуда события

        public string Kcmpnm { get; set; }  // Имя компоненты "BHZ", "BHN", "BHE", "BH1", "BH2"

        // 6. Временные параметры (для идентификации волны P/S)
        public DateTime StartTime { get; set; }     // начало записи
        public float B { get; set; }    // B (сек относительно reference)
        public float A { get; set; }    // A (время прихода P-волны)

        // 7. Качество сигнала

        // Отношение сигнал/шум
        public float SignalToNoiseRatio { get; set; }   
        
        // Флаг качества
        public bool IsGoodQuality { get; set; }     

        // Параметры, рассчитываемые после чтения файла

        // 1. Дистанция до эпицентра (критично для анализа затухания!)
        public double DistanceKm { get; set; }  

        // Азимут от эпицентра к станции (важно для анизотропии)
        public double AzimuthDeg { get; set; } 

        // Амплитуда (расчитываем из данных)
        public float PeakAmplitude { get; set; }     // Пиковая амплитуда (PGV/PGA)
        public float RmsAmplitude { get; set; }      // Среднеквадратичная амплитуда

        private int Nzyear { get; set; }
        private int Nzjday { get; set; }
        private int Nzhour { get; set; }
        private int Nzmin { get; set; }
        private int Nzsec { get; set; }
        private int Nzmsec { get; set; }
        public string DataType => Idep switch
        {
            5 => "displacement",
            6 => "velocity",
            7 => "acceleration",
            50 => "digital",
            _ => $"unknown({Idep})"
        };

        public static SacFile Read(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"Файл не найден: {filename}");

            return ReadFromFile(filename);
        }


        private void ReadHeader(BinaryReader reader)
        {

            // === ЧИТАЕМ FLOAT ПОЛЯ (0-69) ===
            Delta = reader.ReadSingle();      // 0 ⭐ период дискретизации
            reader.ReadSingle();                     // 1: DepMin - пропускаем
            reader.ReadSingle();                     // 2: DepMax - пропускаем  
            reader.ReadSingle();                     // 3: Scale - пропускаем
            reader.ReadSingle();                     // 4: ODelta - пропускаем
            B = reader.ReadSingle();          // 5 ⭐ BeginTimeOffset
            reader.ReadSingle();                     // 6: E - пропускаем
            reader.ReadSingle();                     // 7: O - пропускаем
            A = reader.ReadSingle();          // 8 ⭐ FirstArrivalTime
            reader.ReadSingle();                     // 9: Internal0 - пропускаем

            // Пропускаем T0-T9 (10-19) и F (20-21)
            for (int i = 10; i < 22; i++) reader.ReadSingle();

            // Пропускаем Resp0-Resp9 (22-31)
            for (int i = 22; i < 32; i++) reader.ReadSingle();

            // Станция (32-35)
            Stla = reader.ReadSingle();       // 32 ⭐ Широта станции
            Stlo = reader.ReadSingle();       // 33 ⭐ Долгота станции
            Stel = reader.ReadSingle();       // 34 ⭐ Высота станции
            reader.ReadSingle();                     // 35: StdP - пропускаем

            // Событие (36-39)
            Evla = reader.ReadSingle();       // 36 ⭐ Широта эпицентра
            Evlo = reader.ReadSingle();       // 37 ⭐ Долгота эпицентра
            Evel = reader.ReadSingle();       // 38 ⭐ Высота эпицентра
            Evdp = reader.ReadSingle();       // 39 ⭐ Глубина эпицентра

            // Mag (40)
            Mag = reader.ReadSingle();        // 40 ⭐ Магнитуда события

            // Пропускаем остальные float поля (41-69)
            for (int i = 41; i < 70; i++) reader.ReadSingle();

            // === ЧИТАЕМ INT ПОЛЯ (70-139) ===
            Nzyear = reader.ReadInt32();      // 70 ⭐ Год (для StartTime)
            Nzjday = reader.ReadInt32();      // 71 ⭐ Юлианский день
            Nzhour = reader.ReadInt32();      // 72 ⭐ Час
            Nzmin = reader.ReadInt32();       // 73 ⭐ Минута
            Nzsec = reader.ReadInt32();       // 74 ⭐ Секунда
            Nzmsec = reader.ReadInt32();      // 75 ⭐ Миллисекунда

            // Пропускаем Nvhdr, Norid, Nevid (76-78)
            reader.ReadInt32(); // 76
            reader.ReadInt32(); // 77  
            reader.ReadInt32(); // 78

            Npts = reader.ReadInt32();        // 79 ⭐ Количество точек

            // Пропускаем Nsnpts, Nwfid (80-81)
            reader.ReadInt32(); // 80
            reader.ReadInt32(); // 81

            // Пропускаем Iftype (82)
            reader.ReadInt32(); // 82

            Idep = reader.ReadInt32();        // 83 ⭐ Тип данных

            // Пропускаем Iztype и остальные до ориентации (84-100)
            for (int i = 84; i < 101; i++) reader.ReadInt32();

            // Пропускаем Icmpaz, Icmpinc (101-102)
            reader.ReadInt32(); // 101
            reader.ReadInt32(); // 102

            // Пропускаем остальные int поля (103-139)
            for (int i = 103; i < 140; i++) reader.ReadInt32();

            // === ЧИТАЕМ STRING ПОЛЯ (140-331) ===
            Kstnm = ReadString(reader, 8);    // 140-147 ⭐ Код станции
            reader.ReadBytes(16);                    // 148-163: Kevnm - пропускаем
            reader.ReadBytes(8);                     // 164-171: Khole - пропускаем

            // Пропускаем Ko, Ka, Kt0-Kt9, Kf, Kuser0-Kuser2 (172-299)
            for (int i = 0; i < 16; i++) reader.ReadBytes(8);

            Kcmpnm = ReadString(reader, 8);   // 300-307 ⭐ Имя компоненты

            // Пропускаем Knetwk, Kdatrd (308-323)
            reader.ReadBytes(8); // 308-315
            reader.ReadBytes(8); // 316-323         
        }

        private static float[] ReadData(BinaryReader reader, int length)
        {
            if (length <= 0)
                return Array.Empty<float>();

            try
            {
                float[] data = new float[length];
                for (int i = 0; i < length; i++)
                {
                    if (reader.BaseStream.Position >= reader.BaseStream.Length)
                    {
                        // Файл закончился раньше
                        Array.Resize(ref data, i); // обрезаем массив
                        break;
                    }
                    data[i] = reader.ReadSingle();
                }
                return data;
            }
            catch (EndOfStreamException)
            {
                return Array.Empty<float>();
            }
        }

        private static string ReadString(BinaryReader reader, int length)
        {
            var bytes = reader.ReadBytes(length);
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ');
        }

        private static SacFile ReadFromFile(string filename)
        {
            using var stream = File.OpenRead(filename);
            using var reader = new BinaryReader(stream);

            var sac = new SacFile();
            sac.ReadHeader(reader);
            sac.Data = ReadData(reader, sac.Npts);

            sac.PeakAmplitude = sac.Data.Max(x => Math.Abs(x));
            var doubleData = sac.Data.Select(x => (double)x).ToArray();
            sac.RmsAmplitude = (float)Math.Sqrt(doubleData.Average(x => x * x));
            sac.DistanceKm = sac.FindDistance();
            sac.StartTime = sac.CalculateStartTime();
            return sac;
        }

        public double FindDistance()
        {
            const double R = 6371.0; // радиус Земли в км

            // 1. Переводим в радианы
            double lat1 = this.Stla * Math.PI / 180.0;
            double lon1 = this.Stlo * Math.PI / 180.0;
            double lat2 = this.Evla * Math.PI / 180.0;
            double lon2 = this.Evlo * Math.PI / 180.0;

            // 2. Разницы координат
            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            // 3. Формула гаверсинусов
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            // 4. Угловое расстояние (через atan2 для устойчивости)
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // 5. Линейное расстояние
            return R * c;
        }

        private DateTime CalculateStartTime()
        {
            try
            {
                // Указываем, что время в UTC
                var start = new DateTime(Nzyear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                start = start.AddDays(Nzjday - 1)
                             .AddHours(Nzhour)
                             .AddMinutes(Nzmin)
                             .AddSeconds(Nzsec)
                             .AddMilliseconds(Nzmsec);

                return start;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

    }
}
