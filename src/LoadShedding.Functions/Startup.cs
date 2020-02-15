using LoadShedding.Application.Services;
using LoadShedding.Application.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

[assembly: FunctionsStartup(typeof(LoadShedding.Functions.Startup))]

namespace LoadShedding.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;

            var retryPolicy = HttpPolicyExtensions
              .HandleTransientHttpError()
              .RetryAsync(3);

            services.AddHttpClient<EskomService>(h =>
            {
                h.BaseAddress = new Uri("http://loadshedding.eskom.co.za");
            })
            .AddPolicyHandler(retryPolicy);

            var twilioSid = Environment.GetEnvironmentVariable("TwilioAccountSid");
            var twilioAuthToken = Environment.GetEnvironmentVariable("TwilioAuthToken");
            var cred = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{twilioSid}:{twilioAuthToken}"));
            var twilioSettings = new TwilioSettings
            {
                Sid = twilioSid
            };
            services.AddSingleton(twilioSettings);
            services.AddHttpClient<TwilioService>(h =>
            {
                h.BaseAddress = new Uri("https://api.twilio.com");
                h.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", cred);
            })
            .AddPolicyHandler(retryPolicy);
        }
    }
}
