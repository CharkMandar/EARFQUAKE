using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EARFQUAKE
{
    public class AttenuationAnalysis
    {
        public List<(double DistanceKm, double PeakAmplitude)> DataPoints { get; set; }

        // Метод 1: Линейная регрессия в логарифмических координатах
        public void AnalyzeLogLogPlot()
        {
            // Строим график log(Амплитуда) от log(Расстояние)
            // Если линейно → степенной закон затухания
        }

        // Метод 2: Подбор параметров модели
        public double FitAttenuationModel()
        {
            // A(r) = A0 * r^(-b) * exp(-α*r)
            // где: r - расстояние, b - геометрическое расхождение, 
            // α - коэффициент поглощения
            return 0;
        }
    }
}
