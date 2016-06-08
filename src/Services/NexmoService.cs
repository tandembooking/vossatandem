﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TandemBooking.Services
{
    public class NexmoSmsResult
    {
        [JsonProperty("message-count")]
        public int MessageCount { get; set; }
        public List<NexmoSmsResultMessage> Messages { get; set; }
    }

    public class NexmoSmsResultMessage
    {
        public string Status { get; set; }
        [JsonProperty("message-id")]
        public string MessageId { get; set; }
        public string To { get; set; }
        [JsonProperty("client-ref")]
        public string ClientRef { get; set; }
        [JsonProperty("remaining-balance")]
        public string RemainingBalance { get; set; }
        [JsonProperty("error-text")]
        public string ErrorText { get; set; }
    }

    public class NexmoDeliveryReport
    {
        public string To { get; set; } // set SenderId
        public string NetworkCode { get; set; }
        public string MessageId { get; set; }
        public string Msisdn { get; set; } //recipient phone number
        public string Status { get; set; }
        public string ErrCode { get; set; }
        public string ClientRef { get; set; }
    }

    public class NexmoService
    {
        private const string RestUrl = "https://rest.nexmo.com";
        private const string ApiUrl = "https://api.nexmo.com";

        private readonly HttpClient _client;
        private readonly bool _enable;
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public NexmoService(bool enable, string apiKey, string apiSecret)
        {
            _enable = enable;
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

        public async Task<NexmoSmsResult> SendSms(string from, string to, string text)
        {
            if (!_enable)
            {
                return new NexmoSmsResult()
                {
                    MessageCount = 0,
                    Messages = new List<NexmoSmsResultMessage>(),
                };
            }

            var response = await Post($"{RestUrl}/sms/json", new
            {
                from, to, text
            });
            return response.ToObject<NexmoSmsResult>();
        }

        public async Task<string> FormatPhoneNumber(string phoneNumber, string countryCode = "NO")
        {
            if (!_enable)
            {
                return phoneNumber;
            }

            var result = await Post($"{ApiUrl}/number/format/json", new
            {
                number = phoneNumber,
                country = countryCode
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
                country = countryCode
            });

            return result;
        }
    }
}
