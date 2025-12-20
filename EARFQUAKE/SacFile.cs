using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace EARFQUAKE
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
