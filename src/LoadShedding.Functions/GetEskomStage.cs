using System;
using System.Threading.Tasks;
using LoadShedding.Functions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LoadShedding.Functions
{
    public static class GetEskomStage
    {
        private static readonly EskomService _eskomService = new EskomService();

        [FunctionName("GetEskomStage")]
        public static async Task Run(
            [TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, // once every five minutes
            [Blob("stage-data/current-stage.txt")] CloudBlockBlob currentEskomStage, // update current stage
            [Blob("notifications/stage-changed-people-to-notify.txt")] string peopleToNotify, // new-line separated list of phone numbers
            [TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "+13476090886")] ICollector<CreateMessageOptions> messagesToSend,
            ILogger log)
        {
            // get previous stage
            log.LogInformation($"Getting previous stage");
            var previousStage = int.Parse(await currentEskomStage.DownloadTextAsync());

            // get current stage and save
            log.LogInformation($"Getting current stage");
            var currentStage = await _eskomService.GetEskomStage();
            await currentEskomStage.UploadTextAsync(currentStage.ToString());

            // notify if stage changed
            if (previousStage != currentStage)
            {
                log.LogInformation($"Stage changed from {previousStage} to {currentStage}");

                // send sms's
                var numbers = peopleToNotify.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                log.LogInformation($"Notifying {numbers.Length} people");
                foreach (var number in numbers)
                {
                    messagesToSend.Add(new CreateMessageOptions(new PhoneNumber(number))
                    {
                        Body = $"Loadshedding is now stage {currentStage} (was {previousStage})",
                    });
                }
            }
            else
            {
                log.LogInformation($"Stage unchanged at {currentStage}");
            }
        }
    }
}
