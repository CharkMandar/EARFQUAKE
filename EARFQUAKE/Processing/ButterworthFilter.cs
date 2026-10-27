using System.Numerics;

namespace EARFQUAKE.Processing
{
    /// <summary>
    /// Полосовой фильтр Баттерворта: ФВЧ(lowCut) + ФНЧ(highCut), каждый порядка order,
    /// собранные из биквадратных секций с добротностями Q_k настоящего Баттерворта.
    /// Применяется вперёд и назад (нулевой фазовый сдвиг), поэтому итоговая АЧХ
    /// равна квадрату АЧХ одного прохода: на частоте среза усиление 0.5 (-6 дБ).
    /// </summary>
    public static class ButterworthFilter
    {
        // Публичный интерфейс не изменился
        public static double[] Bandpass(
            double[] signal,
            double samplingRate,
            double lowCut,
            double highCut,
            int order = 4)
        {
            if (signal == null || signal.Length == 0)
                throw new ArgumentException("Сигнал пустой.");

            if (samplingRate <= 0)
                throw new ArgumentException("Частота дискретизации должна быть > 0.");

            if (lowCut <= 0)
                throw new ArgumentException("Нижняя частота должна быть > 0.");

            if (highCut <= lowCut)
                throw new ArgumentException("Верхняя частота должна быть больше нижней.");

            double nyquist = samplingRate / 2.0;

            if (highCut >= nyquist)
                throw new ArgumentException(
                    $"Верхняя частота должна быть меньше частоты Найквиста ({nyquist:F2} Hz).");

            if (order < 2 || order % 2 != 0)
                throw new ArgumentException("Порядок фильтра должен быть чётным и >= 2.");

            var sections = Design(samplingRate, lowCut, highCut, order);

            double[] result = (double[])signal.Clone();

            // Вперёд
            foreach (var s in sections)
                result = Apply(s, result);

            // Назад
            Array.Reverse(result);
            foreach (var s in sections)
                result = Apply(s, result);
            Array.Reverse(result);

            return result;
        }

        /// <summary>
        /// АЧХ всего фильтра (с учётом прохода вперёд и назад) на частоте f.
        /// Нужна для проверки: на lowCut и highCut должно быть около 0.5, в полосе около 1.
        /// </summary>
        public static double MagnitudeResponse(
            double f,
            double samplingRate,
            double lowCut,
            double highCut,
            int order = 4)
        {
            var sections = Design(samplingRate, lowCut, highCut, order);

            Complex z = Complex.FromPolarCoordinates(1.0, 2.0 * Math.PI * f / samplingRate);
            Complex zi = 1.0 / z;

            Complex h = Complex.One;
            foreach (var s in sections)
            {
                Complex num = s.B0 + s.B1 * zi + s.B2 * zi * zi;
                Complex den = 1.0 + s.A1 * zi + s.A2 * zi * zi;
                h *= num / den;
            }

            double gain = h.Magnitude;
            return gain * gain; // вперёд и назад
        }

        // ====================================================================
        // Проектирование
        // ====================================================================

        private readonly record struct Biquad(
            double B0, double B1, double B2, double A1, double A2);

        private static List<Biquad> Design(
            double fs, double lowCut, double highCut, int order)
        {
            var list = new List<Biquad>();

            // Добротности секций Баттерворта чётного порядка N:
            // Q_k = 1 / (2 cos((2k-1)π / (2N))), k = 1..N/2
            // Для N = 4: 0.5412 и 1.3066
            for (int k = 1; k <= order / 2; k++)
            {
                double q = 1.0 / (2.0 * Math.Cos((2.0 * k - 1.0) * Math.PI / (2.0 * order)));

                list.Add(MakeBiquad(highPass: true, fs, lowCut, q));
                list.Add(MakeBiquad(highPass: false, fs, highCut, q));
            }

            return list;
        }

        // Биквад по формулам RBJ (билинейное преобразование с учётом искажения частоты)
        private static Biquad MakeBiquad(bool highPass, double fs, double cutoff, double q)
        {
            double w0 = 2.0 * Math.PI * cutoff / fs;
            double cos = Math.Cos(w0);
            double alpha = Math.Sin(w0) / (2.0 * q);

            double b0, b1, b2;
            if (highPass)
            {
                b0 = (1.0 + cos) / 2.0;
                b1 = -(1.0 + cos);
                b2 = (1.0 + cos) / 2.0;
            }
            else
            {
                b0 = (1.0 - cos) / 2.0;
                b1 = 1.0 - cos;
                b2 = (1.0 - cos) / 2.0;
            }

            double a0 = 1.0 + alpha;
            double a1 = -2.0 * cos;
            double a2 = 1.0 - alpha;

            return new Biquad(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
        }

        private static double[] Apply(Biquad s, double[] x)
        {
            var y = new double[x.Length];
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            for (int i = 0; i < x.Length; i++)
            {
                double x0 = x[i];
                double y0 = s.B0 * x0 + s.B1 * x1 + s.B2 * x2 - s.A1 * y1 - s.A2 * y2;

                y[i] = y0;

                x2 = x1; x1 = x0;
                y2 = y1; y1 = y0;
            }

            return y;
        }
    }
}