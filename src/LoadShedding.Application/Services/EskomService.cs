using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoadShedding.Application.Services
{
    public class EskomService : IEskomService
    {
        private readonly HttpClient _http;

        public EskomService(HttpClient http)
        {
            _http = http;
        }

        // https://github.com/daffster/mypowerstats/blob/master/getshedding.py
        public async Task<int> GetEskomStage()
        {
            var eskomResponse = await _http.GetStringAsync("http://loadshedding.eskom.co.za/LoadShedding/GetStatus");

            // it returns "1" for stage 0, "2" for stage 1, "3" for stage 2 etc
            if (int.TryParse(eskomResponse, out var stage) && stage >= 0)
                return stage - 1;

            throw new ArgumentException($"Cannot convert eskom response {eskomResponse}");
        }
    }
}
