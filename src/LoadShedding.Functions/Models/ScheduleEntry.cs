using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoadShedding.Functions.Models
{
    public class ScheduleEntry : TableEntity
    {
        public int Area => int.Parse(PartitionKey);
        public int Day => int.Parse(RowKey);
        public int StartingHour { get; set; }

        public ScheduleEntry() {}

        public ScheduleEntry(int area, int day, int startingHour)
        {
            PartitionKey = area.ToString();
            RowKey = day.ToString();
            StartingHour = startingHour;
        }
    }
}
