using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LoadShedding.Functions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Polly;

namespace LoadShedding.Functions
{
    public static class FetchEskomStage
    {
        private static readonly HttpClient _http = new HttpClient();

        [FunctionName("FetchEskomStage")]
        public static async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer,                         // once per hour
            [Table("EskomStageHistory")] CloudTable eskomStageHistoryTable,          // save entry in history
            [Blob("current-stage/eskom.txt")] CloudBlockBlob currentEskomStage,      // update current stage
            [Queue(Queues.StageChanged)] ICollector<StageChanged> stageChangedQueue, // notify if stage changed
            ILogger log)
        {
            // get previous stage
            log.LogInformation($"Getting previous stage");
            var previousStage = int.Parse(await currentEskomStage.DownloadTextAsync());

            // get current stage
            log.LogInformation($"Getting current stage");
            var currentStage = await GetEskomStage();
            var entity = new EskomStage(now: DateTimeOffset.UtcNow, stage: currentStage);

            // save the history
            log.LogInformation($"Saving eskom stage {currentStage}");
            var operation = TableOperation.InsertOrReplace(entity);
            await eskomStageHistoryTable.ExecuteAsync(operation);

            // save current stage
            await currentEskomStage.UploadTextAsync(currentStage.ToString());

            // notify via stage changed queue
            if (previousStage != currentStage)
            {
                log.LogInformation($"Stage changed from {previousStage} to {currentStage}");
                stageChangedQueue.Add(new StageChanged
                {
                    PreviousStage = previousStage,
                    CurrentStage = currentStage,
                });
            }
        }

        // https://github.com/daffster/mypowerstats/blob/master/getshedding.py
        private static async Task<int> GetEskomStage()
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(60),
            });

            var eskomResponse = await retryPolicy.ExecuteAsync(()
                => _http.GetStringAsync("http://loadshedding.eskom.co.za/LoadShedding/GetStatus"));

            // it returns "1" for stage 0, "2" for stage 1, "3" for stage 2 etc
            if (int.TryParse(eskomResponse, out var stage))
                return stage - 1;
            
            throw new ArgumentException($"Cannot convert eskom response {eskomResponse}");
        }
    }
}
