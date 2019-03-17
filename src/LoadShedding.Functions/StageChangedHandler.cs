using System;
using LoadShedding.Functions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LoadShedding.Functions
{
    public static class StageChangedHandler
    {
        [FunctionName("StageChanged")]
        [return: TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "+13476090886")]
        public static void Run(
            [QueueTrigger(Queues.StageChanged)] StageChanged message,
            [Blob("notifications/people-to-notify.txt")] string peopleToNotify,
            ICollector<CreateMessageOptions> sms,
            ILogger log)
        {
            log.LogInformation($"Stage changed from {message.PreviousStage} to {message.CurrentStage}");

            var numbers = peopleToNotify.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            log.LogInformation($"Notifying {numbers.Length} people");
            foreach (var number in numbers)
            {
                sms.Add(new CreateMessageOptions(new PhoneNumber(number))
                {
                    Body = $"Loadshedding is now stage {message.CurrentStage} (was {message.PreviousStage})",
                });
            }
        }
    }
}
