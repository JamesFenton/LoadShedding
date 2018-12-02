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

namespace LoadShedding.Functions
{
    public static class FetchEskomStage
    {
        private static HttpClient _http = new HttpClient();

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
            var eskomResponse = await _http.GetStringAsync("http://loadshedding.eskom.co.za/LoadShedding/GetStatus");

            if (!Enum.TryParse(eskomResponse, true, out EskomLoadSheddingStage eskomStage))
                throw new ArgumentException($"Cannot convert eskom response {eskomResponse}");
            
            return (int)eskomStage - 1;
        }

        private enum EskomLoadSheddingStage
        {
            None = 1,
            Stage1 = 2,
            Stage2 = 3,
            Stage3 = 4,
        }
    }
}
