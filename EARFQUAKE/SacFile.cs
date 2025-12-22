using Newtonsoft.Json;
using System.IO.Compression;

namespace EARTHQUAKE
{
    public class SacLoader
    {
        public List<SacFile> LoadFromJson(string jsonFilePath)
        {
            Console.WriteLine($"Загрузка: {jsonFilePath}");

            // Чтение файла (сжатого или обычного)
            string jsonText;
            if (jsonFilePath.EndsWith(".z"))
            {
                jsonText = ReadCompressedJson(jsonFilePath);
            }
            else
            {
                jsonText = File.ReadAllText(jsonFilePath);
            }

            // Десериализация JSON
            var jsonData = JsonConvert.DeserializeObject<JsonData>(jsonText);

            if (jsonData?.Metadata == null)
            {
                Console.WriteLine("Ошибка: неверный формат JSON");
                return new List<SacFile>();
            }

            // Создание объектов SacFile
            var sacFiles = new List<SacFile>();
            int count = 0;

            foreach (var metadata in jsonData.Metadata)
            {
                try
                {
                    var sacFile = SacFile.FromJson(metadata);
                    sacFiles.Add(sacFile);
                    count++;
                }
                catch { }
            }

            Console.WriteLine($"Загружено: {count} файлов");
            return sacFiles;
        }

        private string ReadCompressedJson(string filePath)
        {
            byte[] compressed = File.ReadAllBytes(filePath);

            using (var input = new MemoryStream(compressed))
            using (var decompressor = new ZLibStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                decompressor.CopyTo(output);
                return System.Text.Encoding.UTF8.GetString(output.ToArray());
            }
        }

        private class JsonData
        {
            [JsonProperty("metadata")]
            public List<Dictionary<string, object>> Metadata { get; set; }

            [JsonProperty("total_files")]
            public int TotalFiles { get; set; }
        }
    }
    public class SacFile
    {
        // Основные поля из JSON
        public string Filename { get; set; }
        public string Station { get; set; }
        public string Channel { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public float[] DataSample { get; set; }
        public float Delta { get; set; }

        // Параметры события (из заголовка SAC)
        public float EventLatitude { get; set; }
        public float EventLongitude { get; set; }
        public float EventDepth { get; set; }

        // Вычисляемые поля
        public double DistanceKm { get; set; }
        public float PeakAmplitude { get; set; }

        // Создание из JSON
        public static SacFile FromJson(Dictionary<string, object> jsonDict)
        {
            var sac = new SacFile();

            // Базовые поля
            sac.Filename = GetString(jsonDict, "filename");
            sac.Station = GetString(jsonDict, "station");
            sac.Channel = GetString(jsonDict, "channel");

            // Координаты
            sac.Latitude = GetFloat(jsonDict, "stla", -12345f);
            sac.Longitude = GetFloat(jsonDict, "stlo", -12345f);

            // Параметры события
            sac.EventLatitude = GetFloat(jsonDict, "evla", 54.9f);
            sac.EventLongitude = GetFloat(jsonDict, "evlo", 153.3f);
            sac.EventDepth = GetFloat(jsonDict, "evdp", 580f);

            // Параметры данных
            sac.Delta = GetFloat(jsonDict, "delta", 0.01f);

            // Данные (если есть в base64)
            if (jsonDict.TryGetValue("data_sample_b64", out var base64Data))
            {
                sac.DataSample = DecodeBase64FloatArray(base64Data.ToString());
                sac.PeakAmplitude = CalculatePeakAmplitude(sac.DataSample);
            }

            // Вычисляем дистанцию
            if (sac.Latitude != -12345f && sac.Longitude != -12345f)
            {
                sac.DistanceKm = CalculateDistance(
                    sac.Latitude, sac.Longitude,
                    sac.EventLatitude, sac.EventLongitude
                );
            }

            return sac;
        }

        // Вычисление дистанции (простая формула гаверсинусов)
        private static double CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            const double R = 6371.0; // Радиус Земли в км

            double lat1Rad = lat1 * Math.PI / 180.0;
            double lon1Rad = lon1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            double lon2Rad = lon2 * Math.PI / 180.0;

            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        // Нахождение максимальной амплитуды
        private static float CalculatePeakAmplitude(float[] data)
        {
            if (data == null || data.Length == 0) return 0;

            float max = float.MinValue;
            float min = float.MaxValue;

            foreach (var value in data)
            {
                if (value > max) max = value;
                if (value < min) min = value;
            }

            // Возвращаем максимальную абсолютную амплитуду
            return Math.Max(Math.Abs(max), Math.Abs(min));
        }

        // Вспомогательные методы
        private static string GetString(Dictionary<string, object> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
        }

        private static float GetFloat(Dictionary<string, object> dict, string key, float defaultValue)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                if (float.TryParse(value.ToString(), out float result))
                    return result;
            }
            return defaultValue;
        }

        private static float[] DecodeBase64FloatArray(string base64String)
        {
            try
            {
                byte[] compressed = Convert.FromBase64String(base64String);
                byte[] decompressed = DecompressZlib(compressed);

                float[] result = new float[decompressed.Length / 4];
                Buffer.BlockCopy(decompressed, 0, result, 0, decompressed.Length);
                return result;
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        private static byte[] DecompressZlib(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            using (var decompressor = new ZLibStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                decompressor.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}