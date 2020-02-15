using LoadShedding.Application.Settings;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoadShedding.Application.Services
{
    public class TwilioService
    {
        private readonly HttpClient _http;
        private readonly TwilioSettings _settings;

        public TwilioService(HttpClient http, TwilioSettings settings)
        {
            _http = http;
            _settings = settings;
        }
        
        public async Task SendSms(string to, string message)
        {
            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", to),
                new KeyValuePair<string, string>("From", _settings.From),
                new KeyValuePair<string, string>("Body", message)
            });

            await _http.PostAsync($"/2010-04-01/Accounts/{_settings.Sid}/Messages.json", data);
        }
    }
}
