using JP_Morgan_POC.Model;
using JP_Morgan_POC.Repositories;

namespace JP_Morgan_POC.Services
{
    public class SalesforceSchemaGuard
    {
        private readonly SalesforceMetadataService _metadataService;
        private readonly SyncControlRepository _syncControlRepository;
        private readonly IConfiguration _config;

        public SalesforceSchemaGuard(
            SalesforceMetadataService metadataService,
            SyncControlRepository syncControlRepository,
            IConfiguration config)
        {
            _metadataService = metadataService;
            _syncControlRepository = syncControlRepository;
            _config = config;
        }

        public async Task<ContactMetadataSnapshot> EnsureValidMetadataAsync(CancellationToken ct)
        {
            var (isEnabled, lastHash, lastRefreshUtc) = await _syncControlRepository.GetAsync();
            if (!isEnabled)
                throw new InvalidOperationException("Sync is disabled.");

            var refreshMinutes = int.TryParse(_config["SalesforceSettings:MetadataRefreshMinutes"], out var m) ? m : 5;
            var forceRefresh = !lastRefreshUtc.HasValue || DateTime.UtcNow - lastRefreshUtc.Value >= TimeSpan.FromMinutes(refreshMinutes);

            var metadata = await _metadataService.GetMetadataAsync(forceRefresh, ct);

            if (string.IsNullOrWhiteSpace(lastHash))
            {
                await _syncControlRepository.UpdateMetadataAsync(metadata.SchemaHash);
                return metadata;
            }

            if (!string.Equals(lastHash, metadata.SchemaHash, StringComparison.Ordinal))
            {
                await _syncControlRepository.DisableAsync("Salesforce Contact schema changed.");
                throw new InvalidOperationException("Salesforce schema drift detected. Sync disabled.");
            }

            return metadata;
        }

        public bool Validate(EmpContactDetailsRow row, ContactMetadataSnapshot metadata, out string error)
        {
            error = "";

            if (!Check("FirstName", row.EmpFirstName, metadata, out error)) return false;
            if (!Check("LastName", row.EmpLastName, metadata, out error)) return false;

            return true;
        }

        private bool Check(string fieldName, string value, ContactMetadataSnapshot metadata, out string error)
        {
            error = "";

            if (!metadata.Fields.TryGetValue(fieldName, out var field))
            {
                error = $"Field {fieldName} not found in Salesforce Contact metadata.";
                return false;
            }

            if (field.Length.HasValue && value != null && value.Length > field.Length.Value)
            {
                error = $"Field {fieldName} length exceeded. Value length {value.Length}, allowed {field.Length.Value}.";
                return false;
            }

            return true;
        }
    }
}
