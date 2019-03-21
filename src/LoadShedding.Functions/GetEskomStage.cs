using System;
using System.Threading.Tasks;
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
        private static readonly TwilioService _twilioService = new TwilioService();

        [FunctionName("GetEskomStage")]
        public static async Task Run(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, // once every five minutes
            [Blob("stage-data/current-stage.txt")] CloudBlockBlob currentEskomStage, // update current stage
            [Blob("notifications/stage-changed-people-to-notify.txt")] string peopleToNotify, // new-line separated list of phone numbers
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

                // send sms's
                var numbers = peopleToNotify.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                log.LogInformation($"Notifying {numbers.Length} people");
                var message = $"Loadshedding is now stage {currentStage} (was {previousStage})";
                foreach (var number in numbers)
                {
                    await _twilioService.SendSms(number, message);
                }
            }
            else
            {
                log.LogInformation($"Stage unchanged at {currentStage}");
            }
        }
    }
}
