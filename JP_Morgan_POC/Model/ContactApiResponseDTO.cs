using System.Text.Json.Serialization;

namespace JP_Morgan_POC.Model
{
    public class ContactApiResponseDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errors")]
        public string[] Errors { get; set; }
    }
}
