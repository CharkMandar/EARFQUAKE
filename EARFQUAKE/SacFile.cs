using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Globalization;

namespace EARFQUAKE
{
    public class SacLoader
    {
        public List<SacFile> LoadFromJson(string jsonFilePath)
        {
            Console.WriteLine($"Загрузка: {jsonFilePath}");

            // --------------------------------------------------------
            // Чтение файла
            // --------------------------------------------------------

            string jsonText;

            if (jsonFilePath.EndsWith(".z", StringComparison.OrdinalIgnoreCase))
            {
                jsonText = ReadCompressedJson(jsonFilePath);
            }
            else
            {
                jsonText = File.ReadAllText(jsonFilePath);
            }

            // --------------------------------------------------------
            // Десериализация JSON
            // --------------------------------------------------------

            JsonData? jsonData;

            try
            {
                jsonData = JsonConvert.DeserializeObject<JsonData>(jsonText);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка десериализации JSON: {ex.Message}");
                return new List<SacFile>();
            }

            if (jsonData?.Records == null)
            {
                Console.WriteLine("Ошибка: неверный формат JSON");
                return new List<SacFile>();
            }

            Console.WriteLine(
                $"Версия формата: {jsonData.FormatVersion ?? "не указана"}"
            );

            Console.WriteLine(
                $"Записей в JSON: {jsonData.Records.Count}"
            );

            // --------------------------------------------------------
            // Создание объектов SacFile
            // --------------------------------------------------------

            var sacFiles = new List<SacFile>();

            int count = 0;

            foreach (var record in jsonData.Records)
            {
                try
                {
                    var sacFile = SacFile.FromJson(
                        record,
                        jsonData.Event ?? new Dictionary<string, object>()
                    );

                    sacFiles.Add(sacFile);
                    count++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Ошибка обработки записи: {ex.Message}"
                    );
                }
            }

            Console.WriteLine($"Загружено: {count} записей");

            return sacFiles;
        }

        // ------------------------------------------------------------
        // Чтение сжатого JSON
        // ------------------------------------------------------------

        private string ReadCompressedJson(string filePath)
        {
            byte[] compressed = File.ReadAllBytes(filePath);

            using (var input = new MemoryStream(compressed))
            using (var decompressor = new ZLibStream(
                       input,
                       CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                decompressor.CopyTo(output);

                return System.Text.Encoding.UTF8.GetString(
                    output.ToArray()
                );
            }
        }

        // ------------------------------------------------------------
        // Структура JSON
        // ------------------------------------------------------------

        private class JsonData
        {
            [JsonProperty("format_version")]
            public string? FormatVersion { get; set; }

            [JsonProperty("event")]
            public Dictionary<string, object>? Event { get; set; }

            [JsonProperty("records")]
            public List<Dictionary<string, object>>? Records { get; set; }

            [JsonProperty("total_records")]
            public int TotalRecords { get; set; }

            [JsonProperty("compression")]
            public string? Compression { get; set; }
        }
    }


    // ========================================================================
    // ОДНА СЕЙСМИЧЕСКАЯ ЗАПИСЬ
    // ========================================================================

    public class SacFile
    {
        // --------------------------------------------------------
        // Идентификация станции
        // --------------------------------------------------------

        public string Network { get; set; } = "";
        public string Station { get; set; } = "";
        public string Location { get; set; } = "";
        public string Channel { get; set; } = "";

        // --------------------------------------------------------
        // Координаты станции
        // --------------------------------------------------------

        public double Latitude { get; set; } = double.NaN;
        public double Longitude { get; set; } = double.NaN;

        // --------------------------------------------------------
        // Параметры события
        // --------------------------------------------------------

        public double EventLatitude { get; set; } = double.NaN;
        public double EventLongitude { get; set; } = double.NaN;
        public double EventDepth { get; set; } = double.NaN;
        public double EventMagnitude { get; set; } = double.NaN;

        // --------------------------------------------------------
        // Временные параметры сигнала
        // --------------------------------------------------------

        public string StartTime { get; set; } = "";

        public double SamplingRate { get; set; } = double.NaN;

        // Delta = интервал между отсчётами
        public double Delta { get; set; } = double.NaN;

        // --------------------------------------------------------
        // Данные сигнала
        // --------------------------------------------------------

        public float[] DataSample { get; set; } = Array.Empty<float>();

        // --------------------------------------------------------
        // Вычисляемые параметры
        // --------------------------------------------------------

        public double DistanceKm { get; set; } = double.NaN;

        public double PeakAmplitude { get; set; } = 0.0;


        // ====================================================================
        // СОЗДАНИЕ SacFile ИЗ JSON
        // ====================================================================

        public static SacFile FromJson(
            Dictionary<string, object> jsonDict,
            Dictionary<string, object> eventDict)
        {
            var sac = new SacFile();

            // --------------------------------------------------------
            // Идентификация записи
            // --------------------------------------------------------

            sac.Network = GetString(
                jsonDict,
                "network"
            );

            sac.Station = GetString(
                jsonDict,
                "station"
            );

            sac.Location = GetString(
                jsonDict,
                "location"
            );

            sac.Channel = GetString(
                jsonDict,
                "channel"
            );

            // --------------------------------------------------------
            // Координаты станции
            // --------------------------------------------------------

            sac.Latitude = GetDouble(
                jsonDict,
                "station_latitude",
                double.NaN
            );

            sac.Longitude = GetDouble(
                jsonDict,
                "station_longitude",
                double.NaN
            );

            // --------------------------------------------------------
            // Параметры события
            // --------------------------------------------------------

            sac.EventLatitude = GetDouble(
                eventDict,
                "latitude",
                double.NaN
            );

            sac.EventLongitude = GetDouble(
                eventDict,
                "longitude",
                double.NaN
            );

            sac.EventDepth = GetDouble(
                eventDict,
                "depth_km",
                double.NaN
            );

            sac.EventMagnitude = GetDouble(
                eventDict,
                "magnitude",
                double.NaN
            );

            // --------------------------------------------------------
            // Сигнал
            // --------------------------------------------------------

            if (jsonDict.TryGetValue(
                    "signal",
                    out var signalObject))
            {
                Dictionary<string, object>? signalDict = null;

                // Newtonsoft обычно десериализует вложенный объект
                // как JObject.
                if (signalObject is JObject signalObjectJson)
                {
                    signalDict =
                        signalObjectJson.ToObject<
                            Dictionary<string, object>>();
                }
                else if (signalObject
                         is Dictionary<string, object> dictionary)
                {
                    signalDict = dictionary;
                }

                if (signalDict != null)
                {
                    // Время начала записи
                    sac.StartTime = GetString(
                        signalDict,
                        "start_time"
                    );

                    // Частота дискретизации
                    sac.SamplingRate = GetDouble(
                        signalDict,
                        "sampling_rate",
                        double.NaN
                    );

                    // ------------------------------------------------
                    // Вычисляем Delta
                    // ------------------------------------------------

                    if (!double.IsNaN(sac.SamplingRate) &&
                        sac.SamplingRate > 0)
                    {
                        sac.Delta = 1.0 / sac.SamplingRate;
                    }

                    // ------------------------------------------------
                    // Данные сигнала
                    // ------------------------------------------------

                    if (signalDict.TryGetValue(
                            "samples_b64",
                            out var base64Data))
                    {
                        sac.DataSample =
                            DecodeBase64FloatArray(
                                base64Data?.ToString()
                            );

                        sac.PeakAmplitude =
                            CalculatePeakAmplitude(
                                sac.DataSample
                            );
                    }
                }
            }

            // --------------------------------------------------------
            // Расстояние от станции до эпицентра
            // --------------------------------------------------------

            if (!double.IsNaN(sac.Latitude) &&
                !double.IsNaN(sac.Longitude) &&
                !double.IsNaN(sac.EventLatitude) &&
                !double.IsNaN(sac.EventLongitude))
            {
                sac.DistanceKm = CalculateDistance(
                    sac.Latitude,
                    sac.Longitude,
                    sac.EventLatitude,
                    sac.EventLongitude
                );
            }

            return sac;
        }


        // ====================================================================
        // РАСЧЁТ РАССТОЯНИЯ
        // ====================================================================

        private static double CalculateDistance(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double R = 6371.0;

            double lat1Rad = lat1 * Math.PI / 180.0;
            double lon1Rad = lon1 * Math.PI / 180.0;

            double lat2Rad = lat2 * Math.PI / 180.0;
            double lon2Rad = lon2 * Math.PI / 180.0;

            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            double a =
                Math.Sin(dLat / 2) *
                Math.Sin(dLat / 2) +

                Math.Cos(lat1Rad) *
                Math.Cos(lat2Rad) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            double c =
                2 *
                Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a)
                );

            return R * c;
        }


        // ====================================================================
        // МАКСИМАЛЬНАЯ АМПЛИТУДА
        // ====================================================================

        private static double CalculatePeakAmplitude(
            float[] data)
        {
            if (data == null ||
                data.Length == 0)
            {
                return 0.0;
            }

            double max = double.MinValue;
            double min = double.MaxValue;

            foreach (var value in data)
            {
                if (value > max)
                    max = value;

                if (value < min)
                    min = value;
            }

            return Math.Max(
                Math.Abs(max),
                Math.Abs(min)
            );
        }


        // ====================================================================
        // ПОЛУЧЕНИЕ STRING
        // ====================================================================

        private static string GetString(
            Dictionary<string, object> dict,
            string key)
        {
            if (!dict.TryGetValue(
                    key,
                    out var value))
            {
                return "";
            }

            return value?.ToString() ?? "";
        }


        // ====================================================================
        // ПОЛУЧЕНИЕ DOUBLE
        // ====================================================================

        private static double GetDouble(
            Dictionary<string, object> dict,
            string key,
            double defaultValue)
        {
            if (!dict.TryGetValue(
                    key,
                    out var value) ||
                value == null)
            {
                return defaultValue;
            }

            // Если значение пришло как JValue
            if (value is JValue jValue)
            {
                try
                {
                    return jValue.ToObject<double>();
                }
                catch
                {
                    return defaultValue;
                }
            }

            // Обычный числовой тип
            try
            {
                return Convert.ToDouble(
                    value,
                    CultureInfo.InvariantCulture
                );
            }
            catch
            {
                // Последняя попытка через строку
                if (double.TryParse(
                        value.ToString(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double result))
                {
                    return result;
                }
            }

            return defaultValue;
        }


        // ====================================================================
        // ДЕКОДИРОВАНИЕ samples_b64
        // ====================================================================

        private static float[] DecodeBase64FloatArray(
            string? base64String)
        {
            if (string.IsNullOrEmpty(base64String))
            {
                return Array.Empty<float>();
            }

            try
            {
                // Base64 -> сжатые байты
                byte[] compressed =
                    Convert.FromBase64String(
                        base64String
                    );

                // ZLib -> исходные float32
                byte[] decompressed =
                    DecompressZlib(compressed);

                // Каждый float32 занимает 4 байта
                if (decompressed.Length % 4 != 0)
                {
                    throw new InvalidDataException(
                        "Размер распакованных данных " +
                        "не кратен 4 байтам."
                    );
                }

                float[] result =
                    new float[decompressed.Length / 4];

                Buffer.BlockCopy(
                    decompressed,
                    0,
                    result,
                    0,
                    decompressed.Length
                );

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка декодирования samples_b64: " +
                    $"{ex.Message}"
                );

                return Array.Empty<float>();
            }
        }


        // ====================================================================
        // ZLIB DECOMPRESSION
        // ====================================================================

        private static byte[] DecompressZlib(
            byte[] compressed)
        {
            using (var input =
                   new MemoryStream(compressed))

            using (var decompressor =
                   new ZLibStream(
                       input,
                       CompressionMode.Decompress))

            using (var output =
                   new MemoryStream())
            {
                decompressor.CopyTo(output);

                return output.ToArray();
            }
        }
    }
}