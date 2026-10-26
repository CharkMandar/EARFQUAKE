using System.Globalization;

namespace EARFQUAKE
{
    public record AnalysisRequest(DateTime TimeUtc, int DurationSeconds, string OutputDir);

    public record AnalysisOutput(List<SacFile> Records, List<StationSpectralResult> Results);

    /// <summary>
    /// Создаёт PythonRunner: sacfetch.exe рядом с программой у пользователя,
    /// Python из .venv при разработке (только Debug).
    /// </summary>
    public static class RunnerFactory
    {
        public static PythonRunner Create()
        {
            string exe = Path.Combine(AppContext.BaseDirectory, "sacfetch", "sacfetch.exe");
            if (File.Exists(exe))
                return new PythonRunner(exe);

#if DEBUG
            return new PythonRunner(
                @"C:\Users\mihas\PycharmProjects\FullSacShit\.venv\Scripts\python.exe",
                @"C:\Users\mihas\source\repos\EARFQUAKE\python\SacProcessor.py");
#else
            throw new FileNotFoundException(
                "Не найден sacfetch\\sacfetch.exe рядом с программой. " +
                "Распакуйте архив целиком и запустите EARFQUAKE.exe из распакованной папки.",
                exe);
#endif
        }
    }

    /// <summary>
    /// Полный цикл: Python (скачивание) → загрузка JSON → очистка → спектральный анализ.
    /// Форма только вызывает RunAsync и показывает результат.
    /// </summary>
    public class AnalysisService
    {
        private readonly PythonRunner _runner;

        public AnalysisService(PythonRunner runner) => _runner = runner;

        public async Task<AnalysisOutput> RunAsync(
            AnalysisRequest request,
            IProgress<string>? progress = null,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(request.OutputDir);

            progress?.Report("Скачивание данных, это может занять несколько минут...");

            string json = await _runner.RunAsync(
                new[]
                {
                    "download",
                    "--time", request.TimeUtc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
                    "--duration", request.DurationSeconds.ToString(CultureInfo.InvariantCulture),
                    "--out", request.OutputDir
                },
                progress, ct);

            // Всё тяжёлое дальше уходит в пул потоков, чтобы окно не зависало
            return await Task.Run(() =>
            {
                progress?.Report("Загрузка записей...");
                var loaded = new SacLoader().LoadFromJson(json);
                var records = new SeismicDataCleaner().RemoveDuplicates(loaded).ToList();
                progress?.Report($"Записей после очистки: {records.Count}");

                var analyzer = new SeismicSpectralAnalyzer();
                var results = new List<StationSpectralResult>();

                for (int i = 0; i < records.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var r = analyzer.AnalyzeSacFile(records[i]);
                    if (r != null)
                        results.Add(r);

                    if ((i + 1) % 10 == 0 || i + 1 == records.Count)
                        progress?.Report($"Спектральный анализ: {i + 1}/{records.Count}");
                }

                progress?.Report($"Готово. Проанализировано записей: {results.Count}");
                return new AnalysisOutput(records, results);
            }, ct);
        }
    }
}
