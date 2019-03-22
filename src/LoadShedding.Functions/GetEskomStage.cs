using System;
using System.Threading.Tasks;
using LoadShedding.Functions.Models;
using LoadShedding.Functions.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LoadShedding.Functions
{
    public static class GetEskomStage
    {
        private static readonly EskomService _eskomService = new EskomService();

        [FunctionName("GetEskomStage")]
        public static async Task Run(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, // once every five minutes
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
