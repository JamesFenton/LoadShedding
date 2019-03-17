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
        private static readonly string _NotificationNumber
            = Environment.GetEnvironmentVariable("NotificationNumber");

        [FunctionName("StageChanged")]
        [return: TwilioSms(AccountSidSetting = "TwilioAccountSid", AuthTokenSetting = "TwilioAuthToken", From = "+13476090886")]
        public static CreateMessageOptions Run(
            [QueueTrigger(Queues.StageChanged)] StageChanged message,
            ILogger log)
        {
            log.LogInformation($"Stage changed from {message.PreviousStage} to {message.CurrentStage}");
            return new CreateMessageOptions(new PhoneNumber(_NotificationNumber))
            {
                Body = $"Loadshedding is now stage {message.CurrentStage} (was {message.PreviousStage})",
            };
        }
    }
}
