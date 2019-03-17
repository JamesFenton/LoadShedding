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
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,                         // once per hour
            [Table("EskomStageHistory")] CloudTable eskomStageHistoryTable,         // save entry in history
            [Blob("current-stage/eskom.txt")] CloudBlockBlob currentEskomStage,     // update current stage
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            
            var stage = await GetEskomStage();

            log.LogInformation($"Saving eskom stage {stage}");
            var entity = new EskomStage(now: DateTimeOffset.UtcNow, stage: stage);

            // save the history
            var operation = TableOperation.InsertOrReplace(entity);
            await eskomStageHistoryTable.ExecuteAsync(operation);

            // save current stage
            await currentEskomStage.UploadTextAsync(stage.ToString());
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
            switch (eskomResponse)
            {
                case "1":
                    return 0;
                case "2":
                    return 1;
                case "3":
                    return 2;
                case "4":
                    return 3;
                case "5":
                    return 4;
            }

            throw new ArgumentException($"Cannot convert eskom response {eskomResponse}");
        }
    }
}
