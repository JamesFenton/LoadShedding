using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LoadShedding.Application.Services
{
    public class TwilioService
    {
        private readonly string _from = "+13476090886";
        private readonly string _sid = Environment.GetEnvironmentVariable("TwilioAccountSid");
        private readonly string _token = Environment.GetEnvironmentVariable("TwilioAuthToken");

        private readonly HttpClient _http;

        public TwilioService(HttpClient http)
        {
            _http = http;

            var cred = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_sid}:{_token}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", cred);
        }
        
        public async Task SendSms(string to, string message)
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_sid}/Messages.json";

            var data = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", to),
                new KeyValuePair<string, string>("From", _from),
                new KeyValuePair<string, string>("Body", message)
            });

            await _http.PostAsync(url, data);
        }
    }
}
