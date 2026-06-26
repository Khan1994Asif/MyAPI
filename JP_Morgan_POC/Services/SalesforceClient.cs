using JP_Morgan_POC.Model;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JP_Morgan_POC.Services
{
    public class SalesforceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SalesforceClient> _logger;
        private readonly SalesforceAuthService _service;
        private readonly SalesforceSettings _settings;


        public SalesforceClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SalesforceClient> logger, SalesforceAuthService service, IOptions<SalesforceSettings> options)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _service = service;
            _settings = options.Value;
        }

        public async Task<List<SalesforceBatchUpsertResponse>> UpsertContactsAsync(
            List<EmpContactDetails> rows,
            CancellationToken ct = default)
        {
            if (rows == null || rows.Count == 0)
            {
                return new List<SalesforceBatchUpsertResponse>();
            }

            var results = new List<SalesforceBatchUpsertResponse>();

            const int batchSize = 200; // Salesforce limit for sObject collections upsert [web:44]

            for (int i = 0; i < rows.Count; i += batchSize)
            {
                var batch = rows.Skip(i).Take(batchSize).ToList();
                var batchResult = await UpsertContactBatchInternalAsync(batch, ct);
                results.Add(batchResult);
            }

            return results;
        }

        private async Task<SalesforceBatchUpsertResponse> UpsertContactBatchInternalAsync(
            List<EmpContactDetails> rows,
            CancellationToken ct)
        {
            _settings.AccessToken = await _service.GetAccessTokenAsync();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AccessToken);

            var endpoint =
                $"{_settings.InstanceUrl}/services/data/{_settings.ApiVersion}/composite/sobjects/Contact/{_settings.ContactExternalIdField}";

            var payload = new SalesforceCollectionUpsertRequest
            {
                AllOrNone = true,
                Records = rows.Select(MapToContactRecord).ToList()
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });

            var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Salesforce batch upsert failed. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return new SalesforceBatchUpsertResponse
                {
                    IsSuccess = false,
                    StatusCode = (int)response.StatusCode,
                    RawResponse = responseContent,
                    Results = new List<SalesforceUpsertResult>()
                };
            }

            var upsertResults = JsonSerializer.Deserialize<List<SalesforceUpsertResult>>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<SalesforceUpsertResult>();

            return new SalesforceBatchUpsertResponse
            {
                IsSuccess = true,
                StatusCode = (int)response.StatusCode,
                RawResponse = responseContent,
                Results = upsertResults
            };
        }

        private SalesforceContactRecord MapToContactRecord(EmpContactDetails row)
        {
            return new SalesforceContactRecord
            {
                Attributes = new SalesforceAttributes
                {
                    Type = "Contact"
                },

                // External ID field
                SQL_Id__c = row.Id,

                // Custom fields
                EmpAddress__c = row.EmpAddress,
                EmpFirstName__c = row.EmpFirstName,
                EmpLastName__c = row.EmpLastName,
                EmpProfile__c = row.EmpProfile,

                // Standard Contact fields
                FirstName = row.EmpFirstName,
                LastName = row.EmpLastName
            };
        }
    }
}
