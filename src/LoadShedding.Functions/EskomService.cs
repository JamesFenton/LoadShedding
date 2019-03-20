using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoadShedding.Functions
{
    public class EskomService
    {
        private static readonly HttpClient _http = new HttpClient();

        // https://github.com/daffster/mypowerstats/blob/master/getshedding.py
        public async Task<int> GetEskomStage()
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
            if (int.TryParse(eskomResponse, out var stage) && stage >= 0)
                return stage - 1;

            throw new ArgumentException($"Cannot convert eskom response {eskomResponse}");
        }
    }
}
