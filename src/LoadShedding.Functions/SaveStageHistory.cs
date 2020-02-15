using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadShedding.Application.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace LoadShedding.Functions
{
    public class SaveStageHistory
    {
        [FunctionName("SaveStageHistory")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer, // once per hour
            [Blob("stage-data/current-stage.txt")] CloudBlockBlob currentEskomStage, // get current stage
            [Table("EskomStageHistory")] CloudTable eskomStageHistoryTable, // save entry in history
            ILogger log)
        {
            // get current stage
            log.LogInformation($"Getting current stage");
            var currentStage = int.Parse(await currentEskomStage.DownloadTextAsync());
            
            // save to history
            log.LogInformation($"Saving eskom stage {currentStage}");
            var entity = new EskomStage(now: DateTimeOffset.UtcNow, stage: currentStage);
            var operation = TableOperation.InsertOrReplace(entity);
            await eskomStageHistoryTable.ExecuteAsync(operation);
        }
    }
}
