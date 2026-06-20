namespace JP_Morgan_POC.Model
{
    public class SalesforceSettings
    {
        public const string SectionName = "SalesforceSettings";

        public string AudienceUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string InstanceUrl { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v60.0"; // Default fallback
    }
}
