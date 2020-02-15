using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using Newtonsoft.Json;
using LoadShedding.Application.Infrastructure;
using LoadShedding.Application.Models;

namespace LoadShedding.Functions
{
    public static class UpdateSchedule
    {
        [FunctionName("UpdateSchedule")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Blob(Blobs.Schedule, FileAccess.Write)] out string scheduleBlob,
            ILogger log)
        {
            var schedule = new List<ScheduleEntry>();

            using (var stream = typeof(UpdateSchedule).Assembly.GetManifestResourceStream("LoadShedding.Functions.Schedule.xlsx"))
            using (var package = new ExcelPackage(stream))
            {
                // each sheet is a stage
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    var stage = int.Parse(sheet.Name.Replace("Stage", ""));

                    // each col is a day
                    for(var col = 3; col <= 18; col++)
                    {
                        var day = int.Parse(sheet.Cells[1, col].GetValue<string>()
                            .Replace("st", "")
                            .Replace("nd", "")
                            .Replace("rd", "")
                            .Replace("th", ""));

                        // each row is a starting hour
                        for(var row = 4; row <= 15; row++)
                        {
                            var startingHour = int.Parse(sheet.Cells[row, 1].GetValue<string>().Replace(":00", ""));
                            var areas = sheet.Cells[row, col].GetValue<string>()
                                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                .Select(int.Parse)
                                .ToList();

                            // potentially multiple areas per time slot
                            foreach (var area in areas)
                            {
                                schedule.Add(new ScheduleEntry(stage: stage,
                                                               day: day,
                                                               startingHour: startingHour,
                                                               area: area));

                                // days 17 to 31 are duplicates of 1 to 16
                                if (day <= 15)
                                {
                                    schedule.Add(new ScheduleEntry(stage: stage,
                                                                   day: day + 16,
                                                                   startingHour: startingHour,
                                                                   area: area));
                                }
                            }
                        }
                    }
                }
            }

            scheduleBlob = JsonConvert.SerializeObject(schedule);
        }
    }
}
