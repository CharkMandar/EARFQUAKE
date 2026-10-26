using System.Diagnostics;
using System.Text;

namespace EARFQUAKE.Interop
{
    /// <summary>
    /// Запускает Python-часть (скрипт или собранный exe), показывает ход работы
    /// и возвращает путь к готовому .json.z файлу.
    /// </summary>
    public class PythonRunner
    {
        private readonly string _executable;
        private readonly string? _scriptPath;

        // Вариант 1 (разработка):  new PythonRunner("python", @"C:\...\SacProcessor.py")
        // Вариант 2 (готовое приложение): new PythonRunner(@"...\sacfetch.exe")
        public PythonRunner(string executable, string? scriptPath = null)
        {
            _executable = executable;
            _scriptPath = scriptPath;
        }

        public async Task<string> RunAsync(
            IEnumerable<string> arguments,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,               // чёрное окно не показываем
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Python сам расставит кавычки правильно, если передавать списком
            if (_scriptPath != null)
                psi.ArgumentList.Add(_scriptPath);
            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

            psi.Environment["PYTHONIOENCODING"] = "utf-8";
            psi.Environment["PYTHONUNBUFFERED"] = "1";   // строки приходят сразу, а не пачкой

            string? resultPath = null;
            var errors = new StringBuilder();

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                if (e.Data.StartsWith("RESULT:"))
                    resultPath = e.Data.Substring("RESULT:".Length).Trim();
                else
                    progress?.Report(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errors.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw;
            }

            process.WaitForExit(); // дочитать остатки вывода

            if (process.ExitCode != 0)
            {
                string text = errors.Length > 0 ? errors.ToString().Trim()
                                                : $"код завершения {process.ExitCode}";
                throw new InvalidOperationException("Python-часть завершилась с ошибкой: " + text);
            }

            if (string.IsNullOrEmpty(resultPath))
                throw new InvalidOperationException("Python-часть не вернула путь к результату.");

            return resultPath;
        }
    }
}