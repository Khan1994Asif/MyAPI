# Salesforce Bulk Sync Implementation Guide

## Overview
This document explains the complete implementation of real-time SQL Server to Salesforce contact synchronization with token caching, bulk operations, and error handling.

---

## Components Implemented

### 1. **Enhanced SalesforceAuthService** (`Services/SalesforceAuthService.cs`)

#### Key Features
- **Thread-Safe Token Caching**: Uses `lock` pattern to prevent multiple concurrent token refresh attempts
- **Automatic Token Refresh**: Checks token expiry before each use
- **Expiry Buffer**: Adds 5-minute buffer to prevent using expired tokens
- **Force Refresh on 401**: Automatically refreshes token on Unauthorized responses

#### Token Caching Logic
```csharp
GetAccessTokenAsync()
├── Fast Path (No Lock): Token is valid? Return it
├── Slow Path (With Lock):
│   ├── Double-check pattern
│   ├── Call RefreshTokenAsync() if needed
│   └── Return cached token
└── IsTokenValid() checks:
	├── Token not null/empty
	└── Expiry time > now + 5-minute buffer
```

#### Usage Example
```csharp
// First call: fetches from Salesforce
var token1 = await _authService.GetAccessTokenAsync(); // HTTP call

// Second call: returns cached token (if not expired)
var token2 = await _authService.GetAccessTokenAsync(); // No HTTP call

// After 401 error:
await _authService.ForceRefreshAsync(); // Force new token
var token3 = await _authService.GetAccessTokenAsync(); // New HTTP call
```

---

### 2. **Enhanced SalesforceClient** (`Services/SalesforceClient.cs`)

#### Three Bulk Upsert Strategies

**Strategy 1: Single Upsert (for 1 record)**
- Method: `UpsertContactByExternalIdAsync(EmpContactDetails contact)`
- Endpoint: `PATCH /services/data/v60.0/sobjects/Contact/SQL_Id__c/{externalId}`
- Use When: Inserting/updating single records from Outbox processor
- Max Records: 1

```csharp
var result = await _salesforceClient.UpsertContactByExternalIdAsync(contact);
if (result.IsSuccess)
{
	Console.WriteLine($"Contact {result.SqlId} synced to SF ID {result.SalesforceId}");
}
else
{
	Console.WriteLine($"Failed: {result.ErrorMessage}");
}
```

**Strategy 2: Collection Upsert (RECOMMENDED for most cases)**
- Method: `UpsertContactsAsync(List<EmpContactDetails> rows)`
- Endpoint: `PATCH /services/data/v60.0/composite/sobjects/Contact/SQL_Id__c`
- Use When: Bulk syncing 2-500 records of same type (Contact)
- Max Records Per Batch: 200 (auto-batches 500+ into multiple calls)
- Efficiency: Most efficient for single object type

```csharp
var response = await _salesforceClient.UpsertContactsAsync(records);
Console.WriteLine($"Created: {response.CreatedCount}");
Console.WriteLine($"Updated: {response.UpdatedCount}");
Console.WriteLine($"Failed: {response.FailedCount}");
```

**Strategy 3: Composite API Batch (for mixed operations)**
- Method: `UpsertContactsCompositeAsync(List<E mpContactDetails> rows)`
- Endpoint: `POST /services/data/v60.0/composite`
- Use When: Mixing different operations (create/update/delete on different objects)
- Max Records Per Batch: 25 (auto-batches larger sets)
- Allows: Different HTTP methods per record, referencing previous results

```csharp
var response = await _salesforceClient.UpsertContactsCompositeAsync(records);
```

#### Error Handling

All methods include:
- **401 Unauthorized Retry**: Automatically refreshes token and retries
- **Per-Record Error Tracking**: Each record gets individual result with error details
- **Batch Continuation**: `AllOrNone=false` ensures partial failures don't stop entire batch
- **Detailed Error Info**: Error messages, affected field names, status codes

---

### 3. **Supporting Data Models** (`Model/EmpContactDetailsDTO.cs`)

#### Key Models

**SalesforceCollectionUpsertRequest**
```json
{
  "allOrNone": false,
  "records": [
	{
	  "attributes": { "type": "Contact" },
	  "SQL_Id__c": 123,
	  "FirstName": "John",
	  "LastName": "Doe",
	  "EmpAddress__c": "123 Main St",
	  "EmpFirstName__c": "John",
	  "EmpLastName__c": "Doe",
	  "EmpProfile__c": "Developer"
	}
  ]
}
```

**SalesforceRecordOperationResult** (per-record result)
```csharp
{
  SqlId = 123,
  SalesforceId = "0031X00000HK", // Salesforce Contact ID
  IsSuccess = true,
  IsCreated = true,
  IsUpdated = false,
  IsFailed = false,
  ErrorMessage = "",
  ErrorFields = new List<string>()
}
```

**SalesforceBulkOperationResponse** (overall batch result)
```csharp
{
  IsSuccess = true,
  Message = "Processed 100 records: 95 succeeded, 5 failed",
  RecordResults = [ ... ],
  TotalRecords = 100,
  CreatedCount = 45,
  UpdatedCount = 50,
  FailedCount = 5
}
```

---

## How to Use the Implementation

### Step 1: Configure Salesforce Settings (appsettings.json)
```json
{
  "SalesforceSettings": {
	"LoginUrl": "https://login.salesforce.com",
	"InstanceUrl": "https://yourorg.my.salesforce.com",
	"ClientId": "your-client-id",
	"ClientSecret": "your-client-secret",
	"Username": "your-username",
	"Password": "your-password",
	"SecurityToken": "your-security-token",
	"ApiVersion": "v60.0",
	"ContactExternalIdField": "SQL_Id__c"
  }
}
```

### Step 2: Register in Program.cs
```csharp
services.Configure<SalesforceSettings>(
	configuration.GetSection(SalesforceSettings.SectionName));

services.AddHttpClient<SalesforceAuthService>();
services.AddHttpClient<SalesforceClient>();
services.AddScoped<SalesforceAuthService>();
services.AddScoped<SalesforceClient>();
```

### Step 3: From Outbox Processor Worker (Real-time updates)
```csharp
// Example: Sync one Outbox row to Salesforce
var contact = await _db.EmpContactDetails.FindAsync(outboxRow.EmpId);
var result = await _salesforceClient.UpsertContactByExternalIdAsync(contact);

if (result.IsSuccess)
{
	// Update Salesforce ID if it's a new record
	if (result.IsCreated)
	{
		contact.SalesforceId = result.SalesforceId;
		await _db.SaveChangesAsync();
	}
	// Mark Outbox as processed
	await _outboxRepository.MarkProcessedAsync(outboxRow.Id);
}
else
{
	await _outboxRepository.MarkFailedAsync(outboxRow.Id, result.ErrorMessage);
}
```

### Step 4: From CDC Poller / Bulk Sync (Mass updates)
```csharp
// Example: Sync 500 records from CDC or bulk export
var records = await _db.EmpContactDetails
	.Where(r => r.LastModified > lastSyncTime)
	.ToListAsync();

if (records.Count > 0)
{
	var response = await _salesforceClient.UpsertContactsAsync(records);

	_logger.LogInformation(
		"Bulk sync result: Created={Created}, Updated={Updated}, Failed={Failed}",
		response.CreatedCount, response.UpdatedCount, response.FailedCount);

	// Handle individual failures
	var failed = response.RecordResults.Where(r => r.IsFailed).ToList();
	foreach (var failedRecord in failed)
	{
		_logger.LogError(
			"Sync failed for SQL_ID {SqlId}: {Error}",
			failedRecord.SqlId, failedRecord.ErrorMessage);
	}
}
```

---

## Token Caching Details

### Why Token Caching Matters
- **Reduces API calls**: Salesforce has rate limits (15,000 API calls per 24 hours on Enterprise)
- **Improves performance**: Avoids unnecessary HTTP round-trips to auth endpoint
- **Handles concurrency**: Thread-safe locking prevents race conditions

### Token Lifecycle
```
┌─────────────────────────────────────┐
│ GetAccessTokenAsync() called        │
└──────────────┬──────────────────────┘
			   │
		┌──────▼───────┐
		│ Token valid? │  (Expiry > now + 5 min buffer)
		└──┬───────┬───┘
		   │       │
		YES│       │NO
		   │       └─────────────────┐
		   │                         │
	┌──────▼──────┐         ┌────────▼────────┐
	│ Return      │         │ Acquire lock    │
	│ cached      │         │ _tokenLock      │
	│ token ✓     │         └────────┬────────┘
	│ (Fast!)     │                  │
	└─────────────┘         ┌────────▼────────┐
							│ Double-check    │
							│ Token valid? (2)│ (If another
							│                 │  thread refreshed)
							└────┬────────┬───┘
								 │        │
							  YES│        │NO
								 │        └──┐
					┌────────────┐│  ┌────────▼─────┐
					│ Return due │   │ RefreshToken │
					│ to 2nd     │   │ (OAuth call) │
					│ check      │   │ Cache result │
					└────────────┘   └──────────────┘
```

### Monitoring Token State
```csharp
// Check current token status (for debugging)
private bool IsTokenValid()
{
	// Log: token null/empty, expiry time, buffer calculation
	_logger.LogDebug("Token status: Valid={IsValid}, ExpiresAt={Time}, SecUntilExpiry={Seconds}",
		IsTokenValid(), _tokenExpiresAt, 
		(_tokenExpiresAt - DateTime.UtcNow).TotalSeconds);

	return true; // or false
}
```

---

## Salesforce External ID Setup

### Create SQL_Id__c Field on Contact (one-time setup)
1. **Salesforce Org** → **Setup** → **Contact** object
2. **Fields & Relationships** → **New**
3. **Field Type**: Text
4. **Field Length**: 100
5. **Field Name**: SQL_Id__c
6. **Check "External ID"**
7. **Check "Unique"** (ensures idempotent upserts)
8. Save

### Why External ID?
- **Idempotent upserts**: Same SQL_Id__c always maps to same Salesforce Contact
- **Retry-safe**: Retrying failed upserts won't create duplicates
- **Natural key**: Uses your database's primary key (SQL_Id__c = EmpContactDetails.Id)

---

## Batching Strategy

### Collection Upsert Batching (200 per batch)
```csharp
// Input: 550 records
// Automatically batched as:
Batch 1: Records 0-199 (200 records)   → PATCH to /composite/sobjects/Contact/SQL_Id__c
Batch 2: Records 200-399 (200 records) → PATCH to /composite/sobjects/Contact/SQL_Id__c
Batch 3: Records 400-549 (150 records) → PATCH to /composite/sobjects/Contact/SQL_Id__c

// Each returns SalesforceRecordOperationResult
// Results aggregated into SalesforceBulkOperationResponse
```

### Composite API Batching (25 per batch)
```csharp
// Input: 100 records
// Automatically batched as:
Batch 1: Records 0-24 (25 records)    → POST to /composite
Batch 2: Records 25-49 (25 records)   → POST to /composite
Batch 3: Records 50-74 (25 records)   → POST to /composite
Batch 4: Records 75-99 (25 records)   → POST to /composite
```

---

## Error Handling Patterns

### Schema Errors (Block Sync)
```csharp
// Examples: INVALID_FIELD, STRING_TOO_LONG
// Action: Disable sync, alert team to fix schema
{
  "ErrorMessage": "STRING_TOO_LONG: The field FirstName exceeds max length 40"
}
```

### Transient Errors (Retry)
```csharp
// Examples: Network timeout, 503 Service Unavailable, Rate limit
// Action: Retry with exponential backoff
{
  "ErrorMessage": "HTTP 503: Service Temporarily Unavailable"
}
```

### Record-Specific Errors (Skip and Continue)
```csharp
// Example: Duplicate external ID (shouldn't happen with unique constraint)
// Action: Log, skip, process other records
{
  "ErrorMessage": "SQL_Id__c value already exists",
	 "ErrorFields": ["SQL_Id__c"]
}
```

---

## Performance Metrics

### Token Caching Impact
```
Without Caching:
├─ Every UpsertContactsAsync(100 records) makes 1 auth call
├─ 100 daily syncs = 100 auth calls
└─ Cost: 100 API calls

With Caching (token valid 2 hours):
├─ First sync: 1 auth call
├─ Next 119 syncs within 2 hours: 0 auth calls (cached)
├─ 120 syncs total = ~1 auth call
└─ Cost: 1 API call (99% reduction!)
```

### Batch vs Single Operations
```
Upserting 500 contacts:

Single Upsert (1 record at a time):
└─ 500 HTTP PATCH requests

Collection Upsert (200 per batch):
├─ Batch 1: 1 HTTP PATCH (200 records)
├─ Batch 2: 1 HTTP PATCH (200 records)
└─ Batch 3: 1 HTTP PATCH (100 records)
└─ Total: 3 HTTP PATCH requests (166x fewer!)

Composite API (25 per batch):
├─ Batch 1-20: 20 HTTP POST requests
└─ Total: 20 HTTP POST requests (25x fewer than single)
```

---

## Integration with Outbox Pattern

### Workflow
```
1. Application creates EmpContactDetails
   ↓
2. SaveChangesInterceptor inserts row into Outbox table (same transaction)
   ↓
3. OutboxProcessor BackgroundService polls Outbox
   ↓
4. For each unprocessed Outbox row:
   ├─ Read EmpContactDetails
   ├─ Call UpsertContactByExternalIdAsync() [SINGLE UPSERT]
   ├─ Mark Outbox as ProcessedAt
   └─ Log sync status back to EmpContactDetails.SynStatus
   ↓
5. On failure:
   ├─ Increment RetryCount
   ├─ If retries exceeded → Move to DLQ
   └─ Continue with next row
```

---

## Troubleshooting

### Issue: "Token is null or empty"
```csharp
// Causes:
// 1. First call hasn't been made yet
// 2. Token refresh failed with exception (check logs)
// 3. SalesforceSettings not configured

// Solution:
var token = await _authService.GetAccessTokenAsync(); // Triggers refresh
```

### Issue: "401 Unauthorized"
```csharp
// Causes:
// 1. Token expired and refresh failed
// 2. Credentials in appsettings.json incorrect
// 3. Salesforce org's OAuth app deleted

// Automatic handling in UpsertContactByExternalIdAsync:
if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
	await _authService.ForceRefreshAsync();
	// Retry automatically
}
```

### Issue: "STRING_TOO_LONG on EmpAddress field"
```csharp
// Cause: Salesforce reduced field length, but your data wasn't updated
// Solution:
// 1. Check Salesforce Contact.EmpAddress__c max length
// 2. Update database records to fit
// 3. Adjust model's StringLength attribute

// From logs:
{
  "ErrorMessage": "STRING_TOO_LONG: EmpAddress exceeds max 200 characters",
  "ErrorFields": ["EmpAddress__c"]
}
```

### Issue: "Rate limit exceeded"
```csharp
// Cause: Too many API calls in 24-hour window
// Solution:
// 1. Increase batch size (more records per HTTP call)
// 2. Spread syncs across longer time periods
// 3. Request Salesforce to increase API limit

// Retry with exponential backoff (implement with Polly):
var policy = Policy
	.Handle<HttpRequestException>(r => r.Message.Contains("rate"))
	.WaitAndRetryAsync(
		retryCount: 3,
		sleepDurationProvider: attempt => 
			TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

---

## Summary

| Feature | Benefit |
|---------|---------|
| **Thread-Safe Token Caching** | Reduces auth calls by 99%, improves concurrency |
| **Single Upsert** | For real-time, row-by-row sync (low latency) |
| **Collection Batch API** | For bulk sync (166x fewer HTTP calls than single) |
| **Composite API** | For mixed operations, fallback option |
| **Automatic 401 Retry** | Handles token expiry transparently |
| **Per-Record Error Tracking** | Partial failures don't block other records |
| **External ID Upsert** | Idempotent, retry-safe operations |

---

## Next Steps

1. **Run Database Migration**: Enable Outbox table and trigger (if using Outbox pattern)
2. **Configure Salesforce**: Create SQL_Id__c external ID field on Contact
3. **Test Token Caching**: Monitor logs for "Using cached token" vs "Token refreshed"
4. **Load Test**: Validate throughput with 500+ records
5. **Set Up Monitoring**: Alert on sync failures, token refresh errors
6. **Implement CDC/Outbox**: Use with the CDC poller or SaveChanges interceptor
