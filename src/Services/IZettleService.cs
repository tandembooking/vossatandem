using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TandemBooking.Services
{
    public class IZettleSettings
    {
        public string ClientId { get; set; }
        public string Secret { get; set; }
    }

    public class IZettleService
    {
        private readonly IZettleSettings _settings;
        private HttpClient _httpClient;
        private string _token;
        private DateTime _tokenExpire = DateTime.MinValue;

        public IZettleService(IZettleSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
        }

        private async Task<string> GetToken()
        {
            if (_tokenExpire < DateTime.Now)
            {
                var requestValues = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                    new KeyValuePair<string, string>("client_id", _settings.ClientId),
                    new KeyValuePair<string, string>("assertion", _settings.Secret)
                };

                using (var response = await _httpClient.PostAsync("https://oauth.izettle.com/token", new FormUrlEncodedContent(requestValues)))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Error getting iZettle token, got http status {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                    }

                    var result = await response.Content.ReadFromJsonAsync<IZettleTokenResponse>();
                    _token = result.AccessToken;
                    _tokenExpire = DateTime.Now.AddSeconds(result.ExpiresIn / 2);
                }
            }

            return _token;
        }

        public async Task<List<IZettleSubAccount>> GetSubAccounts()
        {
            //var message = new HttpRequestMessage(HttpMethod.Get, "https://purchase.izettle.com/purchases/v2");
            var message = new HttpRequestMessage(HttpMethod.Get, "https://secure.izettle.com/api/resources/subaccounts");
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetToken());
            message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            using (var response = await _httpClient.SendAsync(message))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<IZettleSubAccount>>();
            }
        }

        public async Task<IZettlePurchasesResponse> GetPayments(DateTime? startDate, DateTime? endDate)
        {
            var args = new List<string>();
            if (startDate != null)
            {
                args.Add($"startDate={startDate:yyyy-MM-dd}");
            }
            if (endDate != null)
            {
                args.Add($"endDate={endDate:yyyy-MM-dd}");
            }
            var message = new HttpRequestMessage(HttpMethod.Get, $"https://purchase.izettle.com/purchases/v2?{string.Join("&", args)}");
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetToken());
            message.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            using (var response = await _httpClient.SendAsync(message))
            {
                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions();
                options.Converters.Add(new DateTimeOffsetConverter());
                return await response.Content.ReadFromJsonAsync<IZettlePurchasesResponse>(options);
            }
        }
    }

    public class IZettleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class IZettlePurchasesResponse
    {
        [JsonPropertyName("purchases")]
        public List<IZettlePurchasesResponsePurchase> Purchases { get; set; }
    }

    public class IZettlePurchasesResponsePurchase
    {
        [JsonPropertyName("purchaseUUID")]
        public string PurchaseUUID { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("userDisplayName")]
        public string UserDisplayName{ get; set; }

        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("payments")]
        public List<IZettlePurchasesResponsePayment> Payments { get; set; }

    }

    public class IZettlePurchasesResponsePayment
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }

    public class IZettleSubAccount
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }
        
        [JsonPropertyName("lastName")]
        public string LastName { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; }
        
        [JsonPropertyName("uuid")]
        public string uuid { get; set; }
    }

}
