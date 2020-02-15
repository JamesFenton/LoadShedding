using System;
using System.Threading.Tasks;
using LoadShedding.Application.Infrastructure;
using LoadShedding.Application.Models;
using LoadShedding.Application.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LoadShedding.Functions
{
    public class GetEskomStage
    {
        private readonly IEskomService _eskomService;

        public GetEskomStage(IEskomService eskomService)
        {
            _eskomService = eskomService;
        }


        [FunctionName("GetEskomStage")]
        public async Task Run(
            [TimerTrigger("*/5 * * * * *")] TimerInfo myTimer, // once every five minutes
            [Blob(Blobs.CurrentStage)] CloudBlockBlob currentEskomStage, // update current stage
            [Queue(Queues.Notifications)] ICollector<StageChanged> notificationsQueue,
            ILogger log)
        {
            // get previous stage
            log.LogInformation($"Getting previous stage");
            var previousStage = int.Parse(await currentEskomStage.DownloadTextAsync());

            // get current stage
            log.LogInformation($"Getting current stage");
            var currentStage = await _eskomService.GetEskomStage();

            // notify if stage changed
            if (previousStage != currentStage)
            {
                log.LogInformation($"Stage changed from {previousStage} to {currentStage}. Saving.");
                await currentEskomStage.UploadTextAsync(currentStage.ToString());
                notificationsQueue.Add(new StageChanged
                {
                    PreviousStage = previousStage,
                    CurrentStage = currentStage
                });
            }
            else
            {
                log.LogInformation($"Stage unchanged at {currentStage}");
            }
        }
    }
}
