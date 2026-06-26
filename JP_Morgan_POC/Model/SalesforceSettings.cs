namespace JP_Morgan_POC.Model
{
    public class SalesforceSettings
    {
        public const string SectionName = "SalesforceSettings";

        //public string AudienceUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string InstanceUrl { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v60.0"; // Default fallback
        public string ContactExternalIdField { get; set; } = string.Empty;
        public string MetadataRefreshMinutes { get; set; }
        public string LoginUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SecurityToken { get; set; }
    }
}
