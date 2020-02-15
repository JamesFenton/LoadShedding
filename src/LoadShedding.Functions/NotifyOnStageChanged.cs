using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoadShedding.Application.Infrastructure;
using LoadShedding.Application.Models;
using LoadShedding.Application.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LoadShedding.Functions
{
    public class NotifyOnStageChanged
    {
        private readonly TwilioService _twilioService;

        public NotifyOnStageChanged(TwilioService twilioService)
        {
            _twilioService = twilioService;
        }

        [FunctionName("NotifyOnStageChanged")]
        public async Task Run(
            [QueueTrigger(Queues.Notifications)] StageChanged stageChanged,
            [Blob(Blobs.Schedule)] string scheduleBlob,
            [Blob(Blobs.StageChangedPeopleToNotify)] string peopleToNotifyBlob,
            ILogger log)
        {
            var schedule = JsonConvert.DeserializeObject<List<ScheduleEntry>>(scheduleBlob);
            var peopleToNotify = JsonConvert.DeserializeObject<List<Person>>(peopleToNotifyBlob);

            log.LogInformation($"Notifying {peopleToNotify.Count} people");
            var currentDay = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).Day;
            var currentHour = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).Hour;
            
            // send sms's
            foreach (var person in peopleToNotify)
            {
                var personsSchedule = schedule
                    .Where(s => s.Stage == stageChanged.CurrentStage && s.Area == person.Area && s.Day == currentDay && s.StartingHour >= currentHour)
                    .OrderBy(x => x.StartingHour)
                    .ToList();
                var message = $"Loadshedding is now stage {stageChanged.CurrentStage} (was {stageChanged.PreviousStage}). ";
                if (stageChanged.CurrentStage > 0)
                {
                    if (personsSchedule.Any())
                        message += $"Area {person.Area} at {string.Join(", ", personsSchedule.Select(p => $"{p.StartingHour}:00"))}";
                    else
                        message += $"No more loadsheeding for area {person.Area}";
                }

                await _twilioService.SendSms(person.Number, message);
            }
        }
    }
}
