namespace JP_Morgan_POC.Model
{
    public class SalesforceConfig
    {
        // e.g. https://login.salesforce.com  (or test.salesforce.com for sandbox)
        public string LoginUrl { get; set; }
        public string ClientId { get; set; }   // {{clientId}}
        public string ClientSecret { get; set; }   // {{clientSecret}}
        public string Username { get; set; }   // {{username}}
        public string Password { get; set; }   // {{password}}
        public string SecurityToken { get; set; }   // {{secretToken}}
    }
}
