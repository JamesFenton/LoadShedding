using System;
using System.Collections.Generic;
using System.Text;

namespace LoadShedding.Application.Models
{
    public class ScheduleEntry
    {
        public int Stage { get; set; }
        public int Area { get; set; }
        public int Day { get; set; }
        public int StartingHour { get; set; }

        public ScheduleEntry() {}

        public ScheduleEntry(int stage, int area, int day, int startingHour)
        {
            Stage = stage;
            Area = area;
            Day = day;
            StartingHour = startingHour;
        }
    }
}
