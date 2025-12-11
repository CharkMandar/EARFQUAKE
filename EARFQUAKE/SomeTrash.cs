using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EARFQUAKE
{
    internal class SomeTrash
    {

        public static void AnalyzeSacAscii(string filename)
        {
            var lines = File.ReadAllLines(filename);

            Console.WriteLine($"Всего строк: {lines.Length}");
            Console.WriteLine($"Длина первой строки: {lines[0].Length}");

            // Разделим первую строку
            var firstLineParts = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"Чисел в первой строке: {firstLineParts.Length}");

            // Пробуем прочитать как float
            try
            {
                var numbers = firstLineParts.Select(float.Parse).ToArray();
                Console.WriteLine("✅ Можно прочитать как float");
                Console.WriteLine($"Первые 5 чисел: {string.Join(", ", numbers.Take(5))}");
            }
            catch
            {
                Console.WriteLine("❌ Не получается прочитать как числа");
            }

            // Проверим есть ли текстовые поля
            bool hasText = lines.Any(l => l.Any(c => char.IsLetter(c)));
            Console.WriteLine($"Есть буквы: {hasText}");
        }

        public static void ShowFileSample(string filename, int lines = 20)
        {
            Console.WriteLine($"=== ПЕРВЫЕ {lines} СТРОК ФАЙЛА ===");
            var linesArray = File.ReadLines(filename).Take(lines).ToArray();

            for (int i = 0; i < linesArray.Length; i++)
            {
                Console.WriteLine($"{i,4}: {linesArray[i]}");
            }

            Console.WriteLine("\n=== ПОСЛЕДНИЕ 5 СТРОК ===");
            var allLines = File.ReadAllLines(filename);
            for (int i = Math.Max(0, allLines.Length - 5); i < allLines.Length; i++)
            {
                Console.WriteLine($"{i,4}: {allLines[i]}");
            }
        }

    }

    public class SacAsciiRecord
    {
        public float[] Floats = new float[70];
        public int[] Ints = new int[40];
        public bool[] Logicals = new bool[5];
        public string[] Strings = new string[24];
        public float[] Data;
    }

    public static class SacAsciiParser
    {
        public static SacAsciiRecord ParseSaca(string path)
        {
            // читаем весь файл как текст
            string text = File.ReadAllText(path);

            // делим на токены по любым пробельным символам
            var tokens = text
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            // минимальное количество токенов в хедере:
            // 70 floats + 40 ints + 5 logicals + 24 strings = 139 токенов
            const int headerTokenCount = 70 + 40 + 5 + 24;
            if (tokens.Length < headerTokenCount)
                throw new InvalidDataException($"Файл слишком короткий для ASCII-SAC header: найдено {tokens.Length} токенов, требуется минимум {headerTokenCount}.");

            var rec = new SacAsciiRecord();
            int idx = 0;
            NumberStyles floatStyle = NumberStyles.Float | NumberStyles.AllowThousands;
            var ci = CultureInfo.InvariantCulture;

            // 1) 70 float
            for (int i = 0; i < 70; i++, idx++)
            {
                if (!float.TryParse(tokens[idx], floatStyle, ci, out rec.Floats[i]))
                    throw new InvalidDataException($"Не удалось распарсить float header[{i}] из токена '{tokens[idx]}' (положение {idx}).");
            }

            // 2) 40 int
            for (int i = 0; i < 40; i++, idx++)
            {
                // иногда в ASCII заголовке встречаются числа вида " -12345" или "-12345.00" для undefined.
                // Попробуем сначала как int, а при провале — как float и приведём к int.
                if (!int.TryParse(tokens[idx], NumberStyles.Integer, ci, out rec.Ints[i]))
                {
                    if (float.TryParse(tokens[idx], floatStyle, ci, out float fval))
                    {
                        rec.Ints[i] = (int)fval;
                    }
                    else
                    {
                        throw new InvalidDataException($"Не удалось распарсить int header[{i}] из токена '{tokens[idx]}' (положение {idx}).");
                    }
                }
            }


            // 4) 24 strings (обычно 8 символов каждая, но в ASCII они идут как отдельные токены)
            for (int i = 0; i < 24; i++, idx++)
            {
                // строки в ASCII обычно уже разделены пробелами — просто сохраним токен
                // уберём кавычки, если они есть, и обрежем
                string s = tokens[idx].Trim();
                if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
                    s = s.Substring(1, s.Length - 2);
                rec.Strings[i] = s;
            }

            // теперь idx указывает на начало данных
            // NPTS — это int с относительным индексом 9 в int-массиве (т.к. ints соответствуют позициям 70..109)
            int npts = rec.Ints[9];
            if (npts <= 0)
                throw new InvalidDataException($"Невалидное значение NPTS = {npts}.");
            int remaining = tokens.Length - idx;
            if (remaining < npts)
                throw new InvalidDataException($"Недостаточно числовых токенов для данных: требуется NPTS={npts}, осталось токенов {remaining}.");

            rec.Data = new float[npts];
            for (int i = 0; i < npts; i++, idx++)
            {
                if (!float.TryParse(tokens[idx], floatStyle, ci, out rec.Data[i]))
                {
                    // иногда данные записаны как целые (без точки) — TryParse всё равно их поймает.
                    throw new InvalidDataException($"Не удалось распарсить data[{i}] из токена '{tokens[idx]}' (положение {idx}).");
                }
            }

            return rec;
        }
    }
}
