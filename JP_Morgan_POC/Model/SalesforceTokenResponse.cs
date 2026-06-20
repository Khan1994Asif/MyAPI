using System.Text.Json.Serialization;

namespace JP_Morgan_POC.Model
{
    public class SalesforceTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("instance_url")]
        public string InstanceUrl { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("issued_at")]
        public string IssuedAt { get; set; }
    }
}
