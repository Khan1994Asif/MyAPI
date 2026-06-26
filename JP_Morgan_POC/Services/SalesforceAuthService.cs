using JP_Morgan_POC.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JP_Morgan_POC.Services
{
    public class SalesforceAuthService
    {
        private const string TokenCacheKey = "salesforce_token";
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(2);

        private readonly HttpClient _httpClient;
        private readonly SalesforceSettings _settings;
        private readonly IMemoryCache _memoryCache;

        public SalesforceAuthService(
            HttpClient httpClient,
            IOptions<SalesforceSettings> options,
            IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _memoryCache = memoryCache;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_memoryCache.TryGetValue(TokenCacheKey, out SalesforceTokenCacheItem cachedToken))
            {
                return cachedToken.AccessToken;
            }

            var token = await RefreshTokenAsync();
            return token.AccessToken;
        }

        public async Task<string> GetInstanceUrlAsync()
        {
            if (_memoryCache.TryGetValue(TokenCacheKey, out SalesforceTokenCacheItem cachedToken))
            {
                return cachedToken.InstanceUrl;
            }

            var token = await RefreshTokenAsync();
            return token.InstanceUrl;
        }

        public void RemoveTokenFromCache()
        {
            _memoryCache.Remove(TokenCacheKey);
        }

        private async Task<SalesforceTokenCacheItem> RefreshTokenAsync()
        {
            var tokenUrl = $"{_settings.LoginUrl}/services/oauth2/token";

            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _settings.ClientId },
                { "client_secret", _settings.ClientSecret },
                { "username", _settings.Username },
                { "password", _settings.Password + _settings.SecurityToken }
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

            var result = JsonSerializer.Deserialize<SalesforceTokenResponse>(body)
                         ?? throw new Exception("Token response is invalid.");

            var cacheItem = new SalesforceTokenCacheItem
            {
                AccessToken = result.AccessToken,
                InstanceUrl = result.InstanceUrl
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TokenLifetime
            };

            _memoryCache.Set(TokenCacheKey, cacheItem, cacheOptions);

            return cacheItem;
        }
    }

    public class SalesforceTokenCacheItem
    {
        public string AccessToken { get; set; } = string.Empty;
        public string InstanceUrl { get; set; } = string.Empty;
    }
}