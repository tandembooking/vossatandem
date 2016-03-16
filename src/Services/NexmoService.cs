using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TandemBooking.Services
{
    public class NexmoService
    {
        private const string RestUrl = "https://rest.nexmo.com";
        private const string ApiUrl = "https://api.nexmo.com";

        private readonly HttpClient _client;
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public NexmoService(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;

            _client = new HttpClient();

            //set auth header
            var byteArray = Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        private async Task<JObject> Post(string url, object messageObject)
        {
            var qs = new QueryString();

            qs = qs.Add("api_key", _apiKey);
            qs = qs.Add("api_secret", _apiSecret);
            qs = qs.Add(
                QueryString.Create(messageObject.GetType()
                    .GetProperties()
                    .Select(
                        x => new KeyValuePair<string, string>(x.Name, x.GetValue(messageObject, null)?.ToString() ?? ""))
                    )
                );

            var result = await _client.GetAsync($"{url}{qs.Value}");
            var data = JsonConvert.DeserializeObject<JObject>(await result.Content.ReadAsStringAsync());

            return data;
        }

        public async Task<JObject> SendSms(string from, string to, string text)
        {
            return await Post($"{RestUrl}/sms/json", new
            {
                from = from,
                to = to,
                text = text,
            });
        }

        public async Task<string> FormatPhoneNumber(string phoneNumber, string countryCode = "NO")
        {
            var result = await Post($"{ApiUrl}/number/format/json", new
            {
                number = phoneNumber,
                country = countryCode,
            });

            int status = result.Value<int>("status");
            if (status != 0)
            {
                return null;
            }
            return result.Value<string>("international_format_number");
        }

        public async Task<JObject> LookupPhoneNumber(string phoneNumber, string countryCode = "NO")
        {
            var result = await Post($"{ApiUrl}/number/lookup/json", new
            {
                number = phoneNumber,
                country = countryCode,
            });

            return result;
        }
    }
}
