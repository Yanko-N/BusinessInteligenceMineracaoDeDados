using System.Globalization;
using Persistence.Enums;
using Persistence.Models;

namespace Application.Core
{
    public static class CsvParser
    {
        public static List<Sale> ParseCsv(string filePath)
        {
            var sales = new List<Sale>();
            var lines = File.ReadAllLines(filePath);

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 5) continue;

                if (!int.TryParse(parts[0].Trim(), out var transaction)) continue;
                if (!DateTime.TryParse(parts[2].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime)) continue;

                var periodDay = parts[3].Trim().ToLowerInvariant() switch
                {
                    "afternoon" => PeriodDay.Afternoon,
                    "evening" => PeriodDay.Evening,
                    "night" => PeriodDay.Night,
                    _ => PeriodDay.Morning
                };

                var typeOfDay = parts[4].Trim().ToLowerInvariant() switch
                {
                    "weekend" => TypeOfDay.Weekend,
                    _ => TypeOfDay.Weekday
                };

                sales.Add(new Sale
                {
                    Transaction = transaction,
                    Item = parts[1].Trim(),
                    DateTime = dateTime,
                    PeriodDay = periodDay,
                    TypeOfDay = typeOfDay
                });
            }

            return sales;
        }
    }
}
