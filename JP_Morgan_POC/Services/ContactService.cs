using Azure.Core;
using JP_Morgan_POC.Controllers;
using JP_Morgan_POC.Model;
using JP_Morgan_POC.Repositories;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JP_Morgan_POC.Services
{
    public class ContactService : IContactService
    {
        private readonly IContactRepository _contactRepository;
        private readonly SalesforceSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly SalesforceAuthService _service;
        public ContactService(IContactRepository contactRepository,
            IOptions<SalesforceSettings> options,
            HttpClient httpClient,
            SalesforceAuthService service)
        {
            _contactRepository = contactRepository;
            _settings = options.Value;
            _httpClient = httpClient;
            _service = service;
        }

        public async Task<IEnumerable<EmpContactDetails>> GetAllContactsAsync()
        {
            //var results = await _service.GetAccessTokenAsync();
            return await _contactRepository.GetAllAsync();
        }

        public async Task<EmpContactDetails?> GetContactByIdAsync(int id)
        {
            return await _contactRepository.GetByIdAsync(id);
        }

        public async Task<EmpContactDetails> CreateContactAsync(EmpContactDetails contact)
        {
            var result = await GetContactsFieldsAndVerify();
            if (result != null && result.Count > 0)
            {
                contact.SynStatus = string.Join(" | ", result);
                await _contactRepository.AddAsync(contact);
                await _contactRepository.SaveChangesAsync();
            }
            else
            {
                contact.SynStatus = "Pending";
                await _contactRepository.AddAsync(contact);
                await _contactRepository.SaveChangesAsync();

                var data = await CreateContact(contact);
            }

            return contact;
        }
        private async Task<List<string>> GetContactsFieldsAndVerify()
        {
            var errorList = new List<string>();
            string endpoint = $"{_settings.InstanceUrl}/services/data/{_settings.ApiVersion}/sobjects/Contact/describe";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            // Supply your OAuth Access Token
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

            // Convert to dictionary for efficient O(1) matching
            var sfFieldsDict = sfSchema.Fields.ToDictionary(f => f.Label, f => f, StringComparer.OrdinalIgnoreCase);

            // 2. Reflect across the C# Entity to inspect types and lengths
            var entityProperties = typeof(EmpContactDetailsDTO).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in entityProperties)
            {
                // Skip tracking fields that don't match Salesforce (like internal database PKs)
                if (property.Name == "Id") continue;

                string fieldName = property.Name;

                // Check if the field even exists in Salesforce anymore
                if (!sfFieldsDict.TryGetValue(fieldName, out var sfField))
                {
                    errorList.Add($"Field '{fieldName}' missing entirely from Salesforce schema.");
                    continue;
                }

                // A. Validate Data Type
                var typeAttr = property.GetCustomAttribute<SalesforceTypeAttribute>();
                if (typeAttr != null && !typeAttr.TargetType.Equals(sfField.Type, StringComparison.OrdinalIgnoreCase))
                {
                    errorList.Add($"Type mismatch on '{fieldName}': SQL expects '{typeAttr.TargetType}', Salesforce is '{sfField.Type}'.");
                }

                // B. Validate Field Size / String Length
                var lengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
                if (lengthAttr != null)
                {
                    int expectedLocalLength = lengthAttr.MaximumLength;
                    int actualSfLength = sfField.Length;

                    // CRITICAL FAIL: If Salesforce size shrinks or expand below what your local C# system allows,
                    // data truncation errors will happen when syncing.
                    if (actualSfLength < expectedLocalLength || actualSfLength > expectedLocalLength)
                    {
                        errorList.Add($"Size conflict on '{fieldName}': SQL limits to '{expectedLocalLength}', but Salesforce Admin altered size to '{actualSfLength}'. Sync data will truncate.");
                    }
                }

            }
            return errorList;
        }

        private async Task<string> CreateContact(EmpContactDetails contact)
        {
            try
            {
                var createContact = new CreateContactDTO()
                {
                    SQL_Id__c = contact.Id.ToString(),
                    EmpAddress__c = contact.EmpAddress,
                    EmpFirstName__c = contact.EmpFirstName,
                    EmpLastName__c = contact.EmpLastName,
                    EmpProfile__c = contact.EmpProfile,
                    FirstName = contact.EmpFirstName,
                    LastName = contact.EmpLastName,
                };
                var json = JsonConvert.SerializeObject(createContact);

                string requestUrl = $"{_settings.InstanceUrl}/services/data/v60.0/sobjects/Contact";
                var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Authorization", $"Bearer {_settings.AccessToken}");
                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    ContactApiResponseDTO responseDto = System.Text.Json.JsonSerializer.Deserialize<ContactApiResponseDTO>(responseString);
                    contact.SynStatus = $"Data Synced | " + responseDto.Id;
                    await _contactRepository.UpdateAsync(contact);
                    await _contactRepository.SaveChangesAsync();
                    return $"Contact created successfully. {responseString}";
                }
                else
                {
                    //return $"Failed to create contact. Status: {response.StatusCode}\nDetails: {responseString}";

                    contact.SynStatus = $"Failed to create contact. Status: {response.StatusCode}\nDetails: {responseString}";
                    await _contactRepository.UpdateAsync(contact);
                    await _contactRepository.SaveChangesAsync();
                    return $"Contact created successfully. {responseString}";
                }

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
    [AttributeUsage(AttributeTargets.Property)]
    public class SalesforceTypeAttribute : Attribute
    {
        public string TargetType { get; }
        public SalesforceTypeAttribute(string targetType) { TargetType = targetType; }
    }
    public class SfFieldMetadata
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public int Length { get; set; } // Salesforce field length (size)
    }

    public class SfDescribeResponse
    {
        public List<SfFieldMetadata> Fields { get; set; }
    }
}
