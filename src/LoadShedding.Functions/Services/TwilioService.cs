using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LoadShedding.Functions.Services
{
    public class TwilioService
    {
        private readonly string _from = "+13476090886";
        private readonly string _sid = Environment.GetEnvironmentVariable("TwilioAccountSid");
        private readonly string _token = Environment.GetEnvironmentVariable("TwilioAuthToken");
        private readonly HttpClient _http = new HttpClient();
        private readonly AsyncRetryPolicy _policy;

        public TwilioService()
        {
            var cred = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_sid}:{_token}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", cred);
            
            _policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                });
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

            await _policy.ExecuteAsync(() => _http.PostAsync(url, data));
        }
    }
}
