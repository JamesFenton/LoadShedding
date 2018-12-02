using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadShedding.Functions.Models
{
    public class EskomStage : TableEntity
    {
        public int Stage { get; set; }

        public EskomStage() {}

        public EskomStage(DateTimeOffset now, int stage)
        {
            PartitionKey = "stage"; // same for all entries
            RowKey = GetNearestHour(now).ToString("o");

            Stage = stage;
        }

        private static DateTimeOffset GetNearestHour(DateTimeOffset now)
        {
            var currentHour = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.FromHours(0));
            var previousHour = currentHour.AddHours(-1);
            var nextHour = currentHour.AddHours(1);

            return new[]
            {
                previousHour,
                currentHour,
                nextHour,
            }.Select(t => (time: t, timeFromNow: Math.Abs((now - t).TotalMilliseconds))) // (time, TimeSpan from now)
            .OrderBy(t => t.timeFromNow) // take the time that is closest to now
            .First()
            .time;
        }
    }
}
