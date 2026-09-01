namespace EARFQUAKE
{
    public class SeismicDataCleaner
    {
        // ====================================================================
        // УДАЛЕНИЕ ТОЧНЫХ ДУБЛИКАТОВ
        // ====================================================================

        public List<SacFile> RemoveDuplicates(
            List<SacFile> records)
        {
            if (records == null ||
                records.Count == 0)
            {
                return new List<SacFile>();
            }

            var uniqueRecords =
                new List<SacFile>();

            foreach (var record in records)
            {
                bool isDuplicate = uniqueRecords.Any(
                    existing =>
                        IsSameRecord(existing, record)
                );

                if (!isDuplicate)
                {
                    uniqueRecords.Add(record);
                }
            }

            return uniqueRecords;
        }


        // ====================================================================
        // ПРОВЕРКА: ЯВЛЯЮТСЯ ЛИ ДВЕ ЗАПИСИ ОДИНАКОВЫМИ
        // ====================================================================

        private bool IsSameRecord(
            SacFile first,
            SacFile second)
        {
            // ------------------------------------------------------------
            // Проверяем основные параметры записи
            // ------------------------------------------------------------

            if (first.Network != second.Network ||
                first.Station != second.Station ||
                first.Location != second.Location ||
                first.Channel != second.Channel ||
                first.StartTime != second.StartTime ||
                first.SamplingRate != second.SamplingRate)
            {
                return false;
            }

            // ------------------------------------------------------------
            // Проверяем количество отсчётов
            // ------------------------------------------------------------

            if (first.DataSample.Length !=
                second.DataSample.Length)
            {
                return false;
            }

            // ------------------------------------------------------------
            // Проверяем сами данные
            // ------------------------------------------------------------

            return first.DataSample.SequenceEqual(
                second.DataSample
            );
        }
    }
}