using JP_Morgan_POC.Model;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace JP_Morgan_POC.Services
{
    public class SalesforceMetadataService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private ContactMetadataSnapshot? _cache;
        private readonly SalesforceSettings _settings;
        private readonly SalesforceAuthService _service;


        public SalesforceMetadataService(HttpClient http, IConfiguration config, IOptions<SalesforceSettings> options, SalesforceAuthService service)
        {
            _http = http;
            _config = config;
            _settings = options.Value;
            _service = service;
        }

        public async Task<ContactMetadataSnapshot> GetMetadataAsync(bool forceRefresh, CancellationToken ct)
        {
            if (!forceRefresh && _cache != null)
                return _cache;

            _settings.AccessToken = await _service.GetAccessTokenAsync();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var url = $"{_settings.InstanceUrl}/services/data/{_settings.ApiVersion}/sobjects/Contact/describe";
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var describe = JsonSerializer.Deserialize<SalesforceDescribeResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var fields = describe.Fields.ToDictionary(
                f => f.Name,
                f => new SalesforceFieldMetadata
                {
                    Name = f.Name,
                    Type = f.Type,
                    Length = f.Length
                },
                StringComparer.OrdinalIgnoreCase);

            _cache = new ContactMetadataSnapshot
            {
                Fields = fields,
                RetrievedAtUtc = DateTime.UtcNow,
                SchemaHash = ComputeHash(fields)
            };

            return _cache;
        }

        private static string ComputeHash(Dictionary<string, SalesforceFieldMetadata> fields)
        {
            var ordered = fields.OrderBy(x => x.Key);
            var sb = new StringBuilder();

            foreach (var field in ordered)
            {
                sb.Append(field.Key)
                  .Append('|')
                  .Append(field.Value.Type)
                  .Append('|')
                  .Append(field.Value.Length?.ToString() ?? "null")
                  .Append(';');
            }

            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }
    }
}
