namespace EARFQUAKE
{
    public static class ButterworthFilter
    {
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
                throw new ArgumentException(
                    "Частота дискретизации должна быть > 0.");

            if (lowCut <= 0)
                throw new ArgumentException(
                    "Нижняя частота должна быть > 0.");

            if (highCut <= lowCut)
                throw new ArgumentException(
                    "Верхняя частота должна быть больше нижней.");

            double nyquist = samplingRate / 2.0;

            if (highCut >= nyquist)
                throw new ArgumentException(
                    $"Верхняя частота должна быть меньше " +
                    $"частоты Найквиста ({nyquist:F2} Hz).");

            if (order < 1)
                throw new ArgumentException(
                    "Порядок фильтра должен быть >= 1.");

            // --------------------------------------------------------
            // Band-pass строим как последовательное применение:
            //
            // high-pass(lowCut)
            //       ↓
            // low-pass(highCut)
            //
            // --------------------------------------------------------

            double[] result = (double[])signal.Clone();

            result = HighPass(
                result,
                samplingRate,
                lowCut,
                order);

            result = LowPass(
                result,
                samplingRate,
                highCut,
                order);

            // --------------------------------------------------------
            // Повторное применение в обратном направлении.
            //
            // Это приближает zero-phase filtering:
            // фильтр не должен сдвигать сигнал по времени.
            // --------------------------------------------------------

            Array.Reverse(result);

            result = HighPass(
                result,
                samplingRate,
                lowCut,
                order);

            result = LowPass(
                result,
                samplingRate,
                highCut,
                order);

            Array.Reverse(result);

            return result;
        }


        // ============================================================
        // LOW-PASS
        // ============================================================

        private static double[] LowPass(
            double[] signal,
            double samplingRate,
            double cutoff,
            int order)
        {
            double[] result = (double[])signal.Clone();

            for (int i = 0; i < order; i++)
            {
                result = LowPassSecondOrder(
                    result,
                    samplingRate,
                    cutoff);
            }

            return result;
        }


        // ============================================================
        // HIGH-PASS
        // ============================================================

        private static double[] HighPass(
            double[] signal,
            double samplingRate,
            double cutoff,
            int order)
        {
            double[] result = (double[])signal.Clone();

            for (int i = 0; i < order; i++)
            {
                result = HighPassSecondOrder(
                    result,
                    samplingRate,
                    cutoff);
            }

            return result;
        }


        // ============================================================
        // LOW-PASS 2-го порядка
        // ============================================================

        private static double[] LowPassSecondOrder(
            double[] signal,
            double samplingRate,
            double cutoff)
        {
            double[] result = new double[signal.Length];

            double omega =
                2.0 * Math.PI * cutoff / samplingRate;

            double cosOmega = Math.Cos(omega);
            double sinOmega = Math.Sin(omega);

            double alpha =
                sinOmega / (2.0 * Math.Sqrt(2.0));

            double b0 =
                (1.0 - cosOmega) / 2.0;

            double b1 =
                1.0 - cosOmega;

            double b2 =
                (1.0 - cosOmega) / 2.0;

            double a0 =
                1.0 + alpha;

            double a1 =
                -2.0 * cosOmega;

            double a2 =
                1.0 - alpha;

            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;

            double x1 = 0.0;
            double x2 = 0.0;
            double y1 = 0.0;
            double y2 = 0.0;

            for (int i = 0; i < signal.Length; i++)
            {
                double x0 = signal[i];

                double y0 =
                    b0 * x0 +
                    b1 * x1 +
                    b2 * x2 -
                    a1 * y1 -
                    a2 * y2;

                result[i] = y0;

                x2 = x1;
                x1 = x0;

                y2 = y1;
                y1 = y0;
            }

            return result;
        }


        // ============================================================
        // HIGH-PASS 2-го порядка
        // ============================================================

        private static double[] HighPassSecondOrder(
            double[] signal,
            double samplingRate,
            double cutoff)
        {
            double[] result = new double[signal.Length];

            double omega =
                2.0 * Math.PI * cutoff / samplingRate;

            double cosOmega = Math.Cos(omega);
            double sinOmega = Math.Sin(omega);

            double alpha =
                sinOmega / (2.0 * Math.Sqrt(2.0));

            double b0 =
                (1.0 + cosOmega) / 2.0;

            double b1 =
                -(1.0 + cosOmega);

            double b2 =
                (1.0 + cosOmega) / 2.0;

            double a0 =
                1.0 + alpha;

            double a1 =
                -2.0 * cosOmega;

            double a2 =
                1.0 - alpha;

            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;

            double x1 = 0.0;
            double x2 = 0.0;
            double y1 = 0.0;
            double y2 = 0.0;

            for (int i = 0; i < signal.Length; i++)
            {
                double x0 = signal[i];

                double y0 =
                    b0 * x0 +
                    b1 * x1 +
                    b2 * x2 -
                    a1 * y1 -
                    a2 * y2;

                result[i] = y0;

                x2 = x1;
                x1 = x0;

                y2 = y1;
                y1 = y0;
            }

            return result;
        }
    }
}