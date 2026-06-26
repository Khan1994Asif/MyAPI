using JP_Morgan_POC.Model;
using JP_Morgan_POC.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JP_Morgan_POC.Services
{
    public class SyncProcessor
    {
        private readonly OutboxRepository _outboxRepository;
        private readonly SalesforceClient _salesforceClient;
        private readonly SalesforceSchemaGuard _schemaGuard;
        private readonly SyncControlRepository _syncControlRepository;
        private readonly SalesforceSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SyncProcessor> _logger;
        private readonly IMemoryCache _cache;
        private readonly SalesforceAuthService _service;
        private const string ContactValidationCacheKey = "salesforce:contact:validation";


        public SyncProcessor(
            OutboxRepository outboxRepository,
            SalesforceClient salesforceClient,
            SalesforceSchemaGuard schemaGuard,
            SyncControlRepository syncControlRepository,
            IOptions<SalesforceSettings> options,
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<SyncProcessor> logger,
            SalesforceAuthService service)
        {
            _outboxRepository = outboxRepository;
            _salesforceClient = salesforceClient;
            _schemaGuard = schemaGuard;
            _syncControlRepository = syncControlRepository;
            _logger = logger;
            _settings = options.Value;
            _httpClient = httpClient;
            _cache = cache;
            _service = service;
        }

        public async Task ProcessAsync(CancellationToken ct)
        {
            //var metadata = await _schemaGuard.EnsureValidMetadataAsync(ct);
            _settings.AccessToken = await _service.GetAccessTokenAsync();
            var outboxBatch = await _outboxRepository.LockPendingBatchAsync(ct);
            if (outboxBatch == null || outboxBatch.Count == 0)
            {
                return;
            }

            var validItems = new List<PendingSyncItem>();

            foreach (var item in outboxBatch)
            {
                var row = item.EmpContactDetails;

                if (row == null)
                {
                    await _outboxRepository.MarkFailedAsync(item.OutboxId, "Source row not found.");
                    continue;
                }

                validItems.Add(new PendingSyncItem
                {
                    OutboxId = item.OutboxId,
                    EmpId = item.EmpId,
                    Row = row
                });
            }


            if (validItems.Count == 0)
            {
                return;
            }

            var realTimeFieldChange = await GetContactsFieldsAndVerify();
            if (realTimeFieldChange == null || realTimeFieldChange.Count == 0)
            {
                List<SalesforceBatchUpsertResponse> batchResponses;
                try
                {
                    batchResponses = await _salesforceClient.UpsertContactsAsync(
                        validItems.Select(x => x.Row).ToList(),
                        ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Salesforce bulk upsert call failed.");

                    foreach (var item in validItems)
                    {
                        await _outboxRepository.MarkFailedAsync(item.OutboxId, ex.Message);
                        await _outboxRepository.UpdateSourceStatusAsync(item.EmpId, "RETRY: " + ex.Message);
                    }

                    return;
                }


                var flattenedResults = batchResponses
                    .SelectMany(x => x.Results)
                    .ToList();

                if (flattenedResults.Count != validItems.Count)
                {
                    var error = $"Salesforce result count mismatch. Sent {validItems.Count}, received {flattenedResults.Count}.";
                    _logger.LogError(error);

                    foreach (var item in validItems)
                    {
                        await _outboxRepository.MarkFailedAsync(item.OutboxId, error);
                        await _outboxRepository.UpdateSourceStatusAsync(item.EmpId, "RETRY: " + error);
                    }

                    return;
                }


                for (int i = 0; i < validItems.Count; i++)
                {
                    var item = validItems[i];
                    var result = flattenedResults[i];

                    if (result.Success)
                    {
                        await _outboxRepository.MarkProcessedAsync(item.OutboxId);
                        await _outboxRepository.UpdateSourceStatusAsync(item.EmpId, "SYNCED");
                        continue;
                    }

                    var errorMessage = BuildSalesforceErrorMessage(result);

                    if (IsSchemaError(result))
                    {
                        await _outboxRepository.MarkBlockedAsync(item.OutboxId, errorMessage);
                        await _outboxRepository.UpdateSourceStatusAsync(item.EmpId, "BLOCKED: Salesforce schema error");
                        await _syncControlRepository.DisableAsync("Salesforce schema-related API error: " + errorMessage);
                        return;
                    }

                    await _outboxRepository.MarkFailedAsync(item.OutboxId, errorMessage);
                    await _outboxRepository.UpdateSourceStatusAsync(item.EmpId, "RETRY: " + errorMessage);
                }
            }
            else
            {
                foreach (var item in validItems)
                {
                    await _outboxRepository.MarkFailedAsync(item.OutboxId, string.Join(" | ", realTimeFieldChange));
                    await _outboxRepository.UpdateSourceStatusAsync(item.EmpId, "RETRY: " + string.Join(" | ", realTimeFieldChange));
                }
            }
        }

        private static bool IsSchemaError(SalesforceUpsertResult result)
        {
            if (result.Errors == null || result.Errors.Count == 0)
                return false;

            var schemaErrorCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "INVALID_FIELD",
                "STRING_TOO_LONG",
                "JSON_PARSER_ERROR",
                "FIELD_INTEGRITY_EXCEPTION"
            };

            return result.Errors.Any(e =>
                !string.IsNullOrWhiteSpace(e.StatusCode) &&
                schemaErrorCodes.Contains(e.StatusCode));
        }

        private static string BuildSalesforceErrorMessage(SalesforceUpsertResult result)
        {
            if (result.Errors == null || result.Errors.Count == 0)
                return "Unknown Salesforce error.";

            return string.Join(" | ",
                result.Errors.Select(e =>
                    $"{e.StatusCode}: {e.Message}" +
                    (e.Fields != null && e.Fields.Count > 0
                        ? $" [Fields: {string.Join(",", e.Fields)}]"
                        : string.Empty)));
        }

        private async Task<List<string>> GetContactsFieldsAndVerify()
        {
            return await _cache.GetOrCreateAsync(ContactValidationCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

                var errorList = new List<string>();
                string endpoint = $"{_settings.InstanceUrl}/services/data/{_settings.ApiVersion}/sobjects/Contact/describe";

                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch Salesforce schema. Status: {response.StatusCode}, Error: {errorContent}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var sfSchema = JsonConvert.DeserializeObject<SfDescribeResponse>(jsonResponse);

                var sfFieldsDict = sfSchema.Fields.ToDictionary(
                    f => f.Label,
                    f => f,
                    StringComparer.OrdinalIgnoreCase);

                var entityProperties = typeof(EmpContactDetailsDTO)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in entityProperties)
                {
                    if (property.Name == "Id")
                        continue;

                    string fieldName = property.Name;

                    if (!sfFieldsDict.TryGetValue(fieldName, out var sfField))
                    {
                        errorList.Add($"Field '{fieldName}' missing entirely from Salesforce schema.");
                        continue;
                    }

                    var typeAttr = property.GetCustomAttribute<SalesforceTypeAttribute>();
                    if (typeAttr != null &&
                        !typeAttr.TargetType.Equals(sfField.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        errorList.Add($"Type mismatch on '{fieldName}': SQL expects '{typeAttr.TargetType}', Salesforce is '{sfField.Type}'.");
                    }

                    var lengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
                    if (lengthAttr != null && sfField.Length != lengthAttr.MaximumLength)
                    {
                        errorList.Add($"Size conflict on '{fieldName}': SQL limits to '{lengthAttr.MaximumLength}', but Salesforce has '{sfField.Length}'.");
                    }
                }

                return errorList;
            });
        }
    }

    public class PendingSyncItem
    {
        public long OutboxId { get; set; }
        public int EmpId { get; set; }
        //public EmpContactDetailsRow Row { get; set; } = default!;
        public EmpContactDetails Row { get; set; }
    }
}