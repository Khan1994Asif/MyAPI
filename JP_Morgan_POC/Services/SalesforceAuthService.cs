using JP_Morgan_POC.Model;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JP_Morgan_POC.Services
{
    public class SalesforceAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly SalesforceConfig _setting;

        private string _accessToken;
        private string _instanceUrl;
        private DateTime _tokenExpiresAt = DateTime.MinValue;

        // Salesforce access tokens typically last 2 hours
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(110);

        public SalesforceAuthService(HttpClient httpClient, IOptions<SalesforceConfig> options)
        {
            _httpClient = httpClient;
            _setting = options.Value;
        }

        /// <summary>
        /// Returns a valid access token. Fetches a new one automatically if expired.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt)
                return _accessToken;

            await RefreshTokenAsync();
            return _accessToken;
        }

        /// <summary>
        /// Returns the Salesforce instance URL (e.g. https://yourorg.my.salesforce.com)
        /// </summary>
        public async Task<string> GetInstanceUrlAsync()
        {
            await GetAccessTokenAsync(); // ensures token is fresh
            return _instanceUrl;
        }

        /// <summary>
        /// Forces a new token fetch regardless of expiry — call this on 401 responses.
        /// </summary>
        public async Task ForceRefreshAsync()
        {
            _tokenExpiresAt = DateTime.MinValue;
            await RefreshTokenAsync();
        }

        private async Task RefreshTokenAsync()
        {
            // Matches your Postman: POST {{url}}{{site}}/services/oauth2/token
            var tokenUrl = $"{_setting.LoginUrl}/services/oauth2/token";

            //var formData = new Dictionary<string, string>
            //{
            //    { "grant_type",    "password"            },
            //    { "client_id",     _setting.ClientId.Trim()      },
            //    { "client_secret", _setting.ClientSecret.Trim()  },
            //    { "username",      _setting.Username.Trim()      },
            //    { "password",      (_setting.Password + _setting.SecurityToken).Trim() }
            //};

            var formData = new Dictionary<string, string>
            {
                { "grant_type",    "client_credentials" },
                { "client_id",     _setting.ClientId.Trim() },
                { "client_secret", _setting.ClientSecret.Trim() }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Salesforce auth failed [{response.StatusCode}]: {body}");

            var result = JsonSerializer.Deserialize<SalesforceTokenResponse>(body);

            _accessToken = result.AccessToken;
            _instanceUrl = result.InstanceUrl;
            _tokenExpiresAt = DateTime.UtcNow.Add(TokenLifetime);
        }
    }
}
