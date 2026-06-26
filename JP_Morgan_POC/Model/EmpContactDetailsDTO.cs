using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JP_Morgan_POC.Model
{
    public class EmpContactDetailsDTO
    {
        [Required]
        [StringLength(200)]
        [SalesforceType("string")]
        public string EmpAddress { get; set; }

        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        public string EmpProfile { get; set; }

        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        public string EmpFirstName { get; set; }

        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        public string EmpLastName { get; set; }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SalesforceTypeAttribute : Attribute
    {
        public string TargetType { get; }
        public SalesforceTypeAttribute(string targetType) { TargetType = targetType; }
    }

    // C# representations matching Salesforce Describe API structures
    public class EmpContactDetailsRow
    {
        public int Id { get; set; }
        public string EmpAddress { get; set; } = "";
        public string EmpProfile { get; set; } = "";
        public string SynStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string EmpFirstName { get; set; } = "";
        public string EmpLastName { get; set; } = "";
    }

    public class OutboxItem
    {
        public long OutboxId { get; set; }
        public int EmpId { get; set; }
        public string EventType { get; set; } = "";
    }

    public class SalesforceDescribeResponse
    {
        public List<SalesforceDescribeField> Fields { get; set; } = new();
    }

    public class SalesforceDescribeField
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int? Length { get; set; }
    }

    public class SalesforceFieldMetadata
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int? Length { get; set; }
    }

    public class ContactMetadataSnapshot
    {
        public string SchemaHash { get; set; } = "";
        public DateTime RetrievedAtUtc { get; set; }
        public Dictionary<string, SalesforceFieldMetadata> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class SalesforceError
    {
        public string ErrorCode { get; set; } = "";
        public string Message { get; set; } = "";
    }

    //public class EmpContactDetails
    //{
    //    public int Id { get; set; }
    //    public string EmpAddress { get; set; } = string.Empty;
    //    public string EmpProfile { get; set; } = string.Empty;
    //    public string SynStatus { get; set; } = string.Empty;
    //    public DateTime CreatedAt { get; set; }
    //    public string EmpFirstName { get; set; } = string.Empty;
    //    public string EmpLastName { get; set; } = string.Empty;
    //}

    public class EmpContactDetailsOutbox
    {
        public long OutboxId { get; set; }
        public int EmpId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime? LockedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }

        public EmpContactDetails EmpContactDetails { get; set; }
    }

    public class SyncControl
    {
        public string EntityName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? LastSchemaHash { get; set; }
        public DateTime? LastMetadataRefreshUtc { get; set; }
        public string? StopReason { get; set; }
    }


public class SalesforceCollectionUpsertRequest
    {
        [JsonPropertyName("allOrNone")]
        public bool AllOrNone { get; set; }

        [JsonPropertyName("records")]
        public List<SalesforceContactRecord> Records { get; set; } = new();
    }

    public class SalesforceContactRecord
    {
        [JsonPropertyName("attributes")]
        public SalesforceAttributes Attributes { get; set; } = new();

        [JsonPropertyName("SQL_Id__c")]
        public int SQL_Id__c { get; set; }

        [JsonPropertyName("EmpAddress__c")]
        public string EmpAddress__c { get; set; } = string.Empty;

        [JsonPropertyName("EmpFirstName__c")]
        public string EmpFirstName__c { get; set; } = string.Empty;

        [JsonPropertyName("EmpLastName__c")]
        public string EmpLastName__c { get; set; } = string.Empty;

        [JsonPropertyName("EmpProfile__c")]
        public string EmpProfile__c { get; set; } = string.Empty;

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("LastName")]
        public string LastName { get; set; } = string.Empty;
    }

    public class SalesforceAttributes
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class SalesforceUpsertResult
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("created")]
        public bool Created { get; set; }

        [JsonPropertyName("errors")]
        public List<SalesforceApiError> Errors { get; set; } = new();
    }

    public class SalesforceApiError
    {
        [JsonPropertyName("statusCode")]
        public string? StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("fields")]
        public List<string> Fields { get; set; } = new();
    }

    public class SalesforceBatchUpsertResponse
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string RawResponse { get; set; } = string.Empty;
        public List<SalesforceUpsertResult> Results { get; set; } = new();
    }
}
