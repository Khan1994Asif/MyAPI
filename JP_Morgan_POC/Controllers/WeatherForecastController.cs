using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JP_Morgan_POC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly HttpClient _httpClient;
        private const string ClientId = "3MVG98_Psg5cppyYCmk1gZNC25o00SXpgpodlS29IZ6pXiHkt3xuPa5qIjBTtEgdsiMuIWVN_8F0jnwEtbDh4";
        private const string Username = "asifkhan86555.8ae6c881fa1b@agentforce.com";

        // Use https://test.salesforce.com if using a Sandbox environment
        private const string AudienceUrl = "https://orgfarm-93b146fa03-dev-ed.develop.my.salesforce.com";//"https://login.salesforce.com";
        private const string TokenUrl = "https://login.salesforce.com/services/oauth2/token";

        string instanceUrl = "https://orgfarm-93b146fa03-dev-ed.develop.my.salesforce.com";
        string accessToken = "00Dg500000CXoQa!AQEAQGpViXIzXBt7yc0p1wMWX9OujOVN5TTomGAHvEECn1o_61uR_3HGaWQkPBMINHuJyLJpqK81imiyVRwtgT22nt0F9OUJ";
        string apiVersion = "v60.0";

        public WeatherForecastController(ILogger<WeatherForecastController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        //[HttpGet(Name = "GetWeatherForecast")]
        [NonAction]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
           

            try
            {
                string endpoint = $"{instanceUrl}/services/data/{apiVersion}/sobjects/Contact/describe";

                using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                // Supply your OAuth Access Token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch Salesforce schema. Status: {response.StatusCode}, Error: {errorContent}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var sfSchema = JsonConvert.DeserializeObject<SfDescribeResponse>(jsonResponse);
                var errors = new List<string>();
                // Convert to dictionary for efficient O(1) matching
                var sfFieldsDict = sfSchema.Fields.ToDictionary(f => f.Label, f => f, StringComparer.OrdinalIgnoreCase);

                // 2. Reflect across the C# Entity to inspect types and lengths
                var entityProperties = typeof(ContactEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in entityProperties)
                {
                    // Skip tracking fields that don't match Salesforce (like internal database PKs)
                    if (property.Name == "Id") continue;

                    string fieldName = property.Name;

                    // Check if the field even exists in Salesforce anymore
                    if (!sfFieldsDict.TryGetValue(fieldName, out var sfField))
                    {
                        errors.Add($"Field '{fieldName}' missing entirely from Salesforce schema.");
                        continue;
                    }

                    // A. Validate Data Type
                    var typeAttr = property.GetCustomAttribute<SalesforceTypeAttribute>();
                    if (typeAttr != null && !typeAttr.TargetType.Equals(sfField.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"Type mismatch on '{fieldName}': Local expects '{typeAttr.TargetType}', Salesforce is '{sfField.Type}'.");
                    }

                    // B. Validate Field Size / String Length
                    var lengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
                    if (lengthAttr != null)
                    {
                        int expectedLocalLength = lengthAttr.MaximumLength;
                        int actualSfLength = sfField.Length;

                        // CRITICAL FAIL: If Salesforce size shrinks below what your local C# system allows,
                        // data truncation errors will happen when syncing.
                        if (actualSfLength < expectedLocalLength || actualSfLength > expectedLocalLength)
                        {
                            errors.Add($"Size conflict on '{fieldName}': Local code limits to '{expectedLocalLength}', but Salesforce Admin altered size to '{actualSfLength}'. Sync data will truncate.");
                        }
                    }


                }

                string contactJsonPayload = @"
                    {
                        ""SQL_Id__c"": 6,
                        ""First_Name__c"": """",
                        ""Last_Name__c"": ""Friday Testing Last name"",
                        ""LastName"": ""Testing user only"",
                        ""FirstName"": ""Hello"",
                        ""Sync_Status__c"": ""Not synced Friday"",
                        ""MobilePhone"": ""7977710150""
                    }";

                //string requestUrl = $"{instanceUrl}/services/data/v60.0/sobjects/Contact";
                await CreateContactAsync(instanceUrl, accessToken, contactJsonPayload);


                return null;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private static async Task CreateContactAsync(string instanceUrl, string accessToken, string jsonPayload)
        {
            using var client = new HttpClient();
            string requestUrl = $"{instanceUrl}/services/data/v60.0/sobjects/Contact";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Contact created successfully via JWT Auth!");
                Console.WriteLine($"Response: {responseString}");
            }
            else
            {
                Console.WriteLine($"Failed to create contact. Status: {response.StatusCode}\nDetails: {responseString}");
            }
        }
    }

    public class ContactEntity
    {
        [Key]
        public int Id { get; set; }

        // Maps to standard Salesforce 'FirstName' (String, Max Length 40)
        [Required]
        [StringLength(100)]
        [SalesforceType("string")] // Custom attribute to track Salesforce expected type
        //[JsonPropertyName("FirstName__c")]
        public string First_Name { get; set; }

        // Maps to standard Salesforce 'LastName' (String, Max Length 80)
        [Required]
        [StringLength(100)]
        [SalesforceType("string")]
        //[JsonPropertyName("LastName__c")]
        public string Last_Name { get; set; }

        // Maps to standard Salesforce 'SyncError' (String, Max Length 80)
        //[StringLength(255)]
        //[SalesforceType("string")]
        //public string SyncError { get; set; }

        // Maps to standard Salesforce 'SyncStatus' (String, Max Length 80)
        [StringLength(255)]
        [SalesforceType("string")]
        public string Sync_Status { get; set; }
    }

    // A simple custom attribute to assist reflection mapping
    [AttributeUsage(AttributeTargets.Property)]
    public class SalesforceTypeAttribute : Attribute
    {
        public string TargetType { get; }
        public SalesforceTypeAttribute(string targetType) { TargetType = targetType; }
    }

    // C# representations matching Salesforce Describe API structures
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
